using System;
using ClothingHygiene.Domain;
using Verse;

namespace ClothingHygiene.Infrastructure.Compat
{
    /// <summary>
    /// 물 공급원 해석. 설정 WaterMode 와 DBH 활성 여부로 결정한다.
    ///
    /// 분기표:
    ///   NoWater                                  → 물 없이
    ///   Auto      + DBH 활성                     → DBH 배관 물
    ///   Auto      + DBH 없음/리플렉션 실패        → 물 없이(조용한 폴백)
    ///   RequireWater + DBH 물 확보               → DBH 배관 물(물이 떨어지면 Available=false)
    ///   RequireWater + DBH 없음/리플렉션 실패     → Available=false(세탁 불가)
    ///
    /// DBH 미설치 경로는 ModLister 조회 1회 + NoWaterSource 반환이 전부다. 틱마다 도는 비용도 로그도 없다.
    /// </summary>
    public static class WaterSourceProvider
    {
        private static readonly NoWaterSource NoWater = new NoWaterSource();
        private static readonly UnavailableWaterSource Unavailable = new UnavailableWaterSource();

        private static IWaterSource current;
        private static WaterMode currentMode = (WaterMode)(-1);

        public static IWaterSource Current
        {
            get
            {
                WaterMode mode = ActiveMode();

                // 설정 창에서 모드를 바꿔도 Reset 호출 없이 다음 접근에서 반영된다(필드 읽기 1회 비용).
                if (current == null || mode != currentMode)
                {
                    current = Resolve(mode);
                    currentMode = mode;
                }

                return current;
            }
        }

        /// <summary>설정이 바뀌면 호출한다(다음 접근에서 다시 해석).</summary>
        public static void Reset()
        {
            current = null;
            currentMode = (WaterMode)(-1);
        }

        private static WaterMode ActiveMode()
        {
            HygieneSettings settings = HygieneMod.Settings;
            return settings != null ? settings.waterMode : WaterMode.Auto;
        }

        private static IWaterSource Resolve(WaterMode mode)
        {
            if (mode == WaterMode.NoWater)
            {
                return NoWater;
            }

            if (IsDubwiseActive())
            {
                DubwiseWaterSource dubwise = DubwiseWaterSource.TryCreate();

                if (dubwise != null)
                {
                    return dubwise;
                }
            }

            // Auto 는 막지 않는 쪽으로(물 없이), RequireWater 는 막는 쪽으로 폴백한다.
            return mode == WaterMode.RequireWater ? (IWaterSource)Unavailable : NoWater;
        }

        /// <summary>packageId 로 DBH 활성 여부를 본다(모드 목록은 세션 중 바뀌지 않으므로 캐시).</summary>
        private static bool IsDubwiseActive()
        {
            try
            {
                return ModLister.GetActiveModWithIdentifier(DubwiseWaterSource.PackageId) != null;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
