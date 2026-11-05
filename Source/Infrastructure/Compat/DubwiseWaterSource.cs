using System;
using System.Collections;
using System.Reflection;
using ClothingHygiene.Domain;
using Verse;

namespace ClothingHygiene.Infrastructure.Compat
{
    /// <summary>
    /// Dubs Bad Hygiene 물 연동. BadHygiene.dll 을 컴파일 참조하지 않고 리플렉션으로만 만진다(하드 의존 금지).
    ///
    /// 실제 물 보유/차감 경로(ilspycmd 로 확인):
    ///   DubsBadHygiene.HygienePipeMapComp.DBHComps (public static Dictionary&lt;int, HygienePipeMapComp&gt;)
    ///     → .PipeNets (public PlumbingNet[])
    ///       → PlumbingNet.WaterStorage  (public float, 탱크 합계)
    ///       → PlumbingNet.PullWater(float, out ContaminationLevel) (public bool, 탱크에서 실제 차감)
    ///
    /// IWaterSource 에는 위치/맵 문맥이 없으므로 "설치된 모든 배관망 중 물이 있는 망"을 집계한다.
    /// 즉 배관에 연결된 물탱크가 있어야 Available 이 true 다.
    /// </summary>
    public class DubwiseWaterSource : IWaterSource
    {
        public const string PackageId = "Dubwise.DubsBadHygiene";
        public const string LabelKeyValue = "CH_Water_Dubwise";

        /// <summary>DBH 물 수치는 float 누적이라 0 취급 임계값을 둔다.</summary>
        private const float WaterEpsilon = 0.001f;

        /// <summary>탱크 총량 캐시 유효 기간(틱). 매 틱 배관망 전체를 다시 합산하지 않기 위함.</summary>
        private const int CacheTicks = 30;

        private static bool failureLogged;

        private int cacheStamp;
        private float cachedWater;
        private bool cacheValid;
        private bool broken;

        private DubwiseWaterSource()
        {
        }

        /// <summary>리플렉션 핸들을 미리 확보한 뒤 생성한다. 실패하면 null(호출측이 조용히 폴백).</summary>
        public static DubwiseWaterSource TryCreate()
        {
            try
            {
                DubwiseReflection.EnsureInitialized();
                return new DubwiseWaterSource();
            }
            catch (Exception ex)
            {
                ReportOnce(ex);
                return null;
            }
        }

        public bool Available
        {
            get
            {
                if (broken)
                {
                    return false;
                }

                RefreshIfStale();
                return cachedWater > WaterEpsilon;
            }
        }

        public string LabelKey
        {
            get { return LabelKeyValue; }
        }

        public bool TryConsume(float amount)
        {
            if (amount <= 0f)
            {
                return true;
            }

            if (broken)
            {
                return false;
            }

            RefreshIfStale();

            if (cachedWater < amount)
            {
                return false;
            }

            try
            {
                if (PullFromFirstNetWithEnough(amount))
                {
                    cachedWater -= amount;

                    if (cachedWater < 0f)
                    {
                        cachedWater = 0f;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                broken = true;
                ReportOnce(ex);
            }

            return false;
        }

        private void RefreshIfStale()
        {
            int tick = GameTick();

            if (cacheValid && tick >= cacheStamp && tick - cacheStamp < CacheTicks)
            {
                return;
            }

            cacheStamp = tick;
            cacheValid = true;
            cachedWater = MeasureAllWater();
        }

        /// <summary>모든 맵의 모든 배관망 탱크 총량. 배관망이 없으면 0.</summary>
        private static float MeasureAllWater()
        {
            IDictionary comps = AllPipeMapComps();

            if (comps == null)
            {
                return 0f;
            }

            float total = 0f;
            IDictionaryEnumerator e = comps.GetEnumerator();

            while (e.MoveNext())
            {
                Array nets = PipeNetsOf(e.Value);

                if (nets == null)
                {
                    continue;
                }

                for (int i = 0; i < nets.Length; i++)
                {
                    object net = nets.GetValue(i);

                    if (net != null)
                    {
                        total += WaterOf(net);
                    }
                }
            }

            return total;
        }

        private static bool PullFromFirstNetWithEnough(float amount)
        {
            IDictionary comps = AllPipeMapComps();

            if (comps == null)
            {
                return false;
            }

            IDictionaryEnumerator e = comps.GetEnumerator();

            while (e.MoveNext())
            {
                Array nets = PipeNetsOf(e.Value);

                if (nets == null)
                {
                    continue;
                }

                for (int i = 0; i < nets.Length; i++)
                {
                    object net = nets.GetValue(i);

                    if (net == null || WaterOf(net) < amount)
                    {
                        continue;
                    }

                    if (Pull(net, amount))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static IDictionary AllPipeMapComps()
        {
            return DubwiseReflection.PipeMapCompsField.GetValue(null) as IDictionary;
        }

        private static Array PipeNetsOf(object pipeMapComp)
        {
            if (pipeMapComp == null)
            {
                return null;
            }

            return DubwiseReflection.PipeNetsField.GetValue(pipeMapComp) as Array;
        }

        private static float WaterOf(object plumbingNet)
        {
            object value = DubwiseReflection.WaterStorageProperty.GetValue(plumbingNet, null);
            return value is float f ? f : 0f;
        }

        /// <summary>PlumbingNet.PullWater(float, out ContaminationLevel) — 탱크에서 실제로 차감한다.</summary>
        private static bool Pull(object plumbingNet, float amount)
        {
            object[] args = { amount, null };
            object result = DubwiseReflection.PullWaterMethod.Invoke(plumbingNet, args);
            return result is bool ok && ok;
        }

        private static int GameTick()
        {
            TickManager tickManager = Find.TickManager;
            return tickManager != null ? tickManager.TicksGame : 0;
        }

        private static void ReportOnce(Exception ex)
        {
            if (failureLogged)
            {
                return;
            }

            failureLogged = true;
            Log.Warning("[ClothingHygiene] Dubs Bad Hygiene water hook disabled, falling back to no-water: " + ex.Message);
        }

        /// <summary>DBH 타입/멤버 핸들. 한 번만 해석하고, 실패하면 호출측이 폴백한다.</summary>
        private static class DubwiseReflection
        {
            private const string AssemblyName = "BadHygiene";
            private const string PlumbingNetTypeName = "DubsBadHygiene.PlumbingNet";
            private const string PipeMapCompTypeName = "DubsBadHygiene.HygienePipeMapComp";

            internal static FieldInfo PipeMapCompsField;
            internal static FieldInfo PipeNetsField;
            internal static PropertyInfo WaterStorageProperty;
            internal static MethodInfo PullWaterMethod;

            private static bool initialized;
            private static bool available;

            internal static void EnsureInitialized()
            {
                if (initialized)
                {
                    if (!available)
                    {
                        throw new InvalidOperationException("Dubs Bad Hygiene plumbing types are unavailable");
                    }

                    return;
                }

                Type netType = FindType(PlumbingNetTypeName);
                Type pipeMapCompType = FindType(PipeMapCompTypeName);

                if (netType == null || pipeMapCompType == null)
                {
                    throw new TypeLoadException("Dubs Bad Hygiene plumbing types not found");
                }

                PipeMapCompsField = pipeMapCompType.GetField("DBHComps", BindingFlags.Public | BindingFlags.Static);
                PipeNetsField = pipeMapCompType.GetField("PipeNets", BindingFlags.Public | BindingFlags.Instance);
                WaterStorageProperty = netType.GetProperty("WaterStorage", BindingFlags.Public | BindingFlags.Instance);
                PullWaterMethod = FindPullWater(netType);

                if (PipeMapCompsField == null || PipeNetsField == null || WaterStorageProperty == null || PullWaterMethod == null)
                {
                    throw new MissingMemberException("Dubs Bad Hygiene plumbing members changed");
                }

                available = true;
                initialized = true;
            }

            /// <summary>PullWater(float, out ContaminationLevel) — out 파라미터 타입(enum)은 이름으로 찾지 않고 byref 여부만 본다.</summary>
            private static MethodInfo FindPullWater(Type netType)
            {
                MethodInfo[] methods = netType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];

                    if (method.Name != "PullWater")
                    {
                        continue;
                    }

                    ParameterInfo[] parameters = method.GetParameters();

                    if (parameters.Length == 2
                        && parameters[0].ParameterType == typeof(float)
                        && parameters[1].ParameterType.IsByRef)
                    {
                        return method;
                    }
                }

                return null;
            }

            private static Type FindType(string fullName)
            {
                Type type = Type.GetType(fullName + ", " + AssemblyName, false);

                if (type != null)
                {
                    return type;
                }

                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

                for (int i = 0; i < assemblies.Length; i++)
                {
                    type = assemblies[i].GetType(fullName, false);

                    if (type != null)
                    {
                        return type;
                    }
                }

                // 마지막 시도: DBH 어셈블리가 아직 로드 전이면 로드를 요청한다(없으면 예외 → 호출측 폴백).
                try
                {
                    return Assembly.Load(new AssemblyName(AssemblyName)).GetType(fullName, false);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }
    }
}
