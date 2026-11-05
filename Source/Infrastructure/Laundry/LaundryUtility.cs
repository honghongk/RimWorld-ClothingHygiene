using System.Collections.Generic;
using ClothingHygiene.Domain;
using ClothingHygiene.Infrastructure.Compat;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ClothingHygiene.Infrastructure
{
    /// <summary>
    /// 세탁 판정·결과 적용. 대상 탐색은 폰이 세탁 작업을 시작할 때(또는 세탁기 주기마다)만 불리며,
    /// 틱 루프에서 부르지 않는다. 정적 캐시는 ThingDef 조회 1회뿐이다.
    /// </summary>
    public static class LaundryUtility
    {
        /// <summary>이 값 이상이면 세탁 대상으로 본다(너무 자주 빨지 않도록 25%).</summary>
        public const float DirtyThreshold = 0.25f;

        private const string SoapDefName = "CH_Soap";

        private static ThingDef soapDef;
        private static bool soapLookupDone;

        public static ThingDef SoapDef
        {
            get
            {
                if (!soapLookupDone)
                {
                    soapDef = DefDatabase<ThingDef>.GetNamed(SoapDefName, false);
                    soapLookupDone = true;
                }

                return soapDef;
            }
        }

        /// <summary>
        /// 물 사용 가능 여부. RequireWater 모드에서만 물을 요구하고, NoWater/Auto 스텁에서는 항상 통과한다.
        /// </summary>
        public static bool WaterAvailable()
        {
            HygieneSettings settings = HygieneMod.Settings;
            if (settings == null || settings.waterMode != WaterMode.RequireWater)
            {
                return true;
            }

            IWaterSource source = WaterSourceProvider.Current;
            return source == null || source.Available;
        }

        /// <summary>물을 소비한다. 소비하지 못하면 세탁 자체를 하지 않는다.</summary>
        public static bool ConsumeWater(float units)
        {
            if (!WaterAvailable())
            {
                return false;
            }

            IWaterSource source = WaterSourceProvider.Current;
            return source == null || source.TryConsume(units);
        }

        public static float SoaplessFloor()
        {
            HygieneSettings settings = HygieneMod.Settings;
            return settings != null ? settings.soaplessCleanFloor : 0.4f;
        }

        /// <summary>
        /// 세탁이 실제로 값을 낮출 수 있는 최소 오염도. 세제가 없으면 바닥값 아래로는 못 내리므로
        /// 그 위쪽만 대상으로 삼는다(같은 옷을 무한히 다시 빨지 않게 하는 장치).
        /// </summary>
        public static float WashThreshold(bool withSoap)
        {
            if (withSoap)
            {
                return DirtyThreshold;
            }

            float floor = SoaplessFloor() + 0.05f;
            return floor > DirtyThreshold ? floor : DirtyThreshold;
        }

        /// <summary>세탁 결과 적용: 세제가 있으면 0, 없으면 soaplessCleanFloor 까지만. 내구도도 함께 소모한다.</summary>
        public static void ApplyWash(Thing garment, ThingComp_ClothingHygiene hygiene, bool usedSoap)
        {
            if (garment == null || hygiene == null)
            {
                return;
            }

            hygiene.Filth = usedSoap ? HygieneValue.Min : Mathf.Min(hygiene.Filth, SoaplessFloor());
            ApplyDurabilityLoss(garment);
        }

        /// <summary>세탁 1회당 설정 비율만큼 HitPoints 를 깎는다. 최소 1 은 남긴다.</summary>
        public static void ApplyDurabilityLoss(Thing garment)
        {
            HygieneSettings settings = HygieneMod.Settings;
            float percent = settings != null ? settings.laundryDurabilityLossPercent : 2f;
            if (percent <= 0f || garment.def == null || !garment.def.useHitPoints)
            {
                return;
            }

            int max = garment.MaxHitPoints;
            int loss = Mathf.RoundToInt(max * percent / 100f);
            if (loss < 1)
            {
                loss = 1;
            }

            int hp = garment.HitPoints - loss;
            garment.HitPoints = hp < 1 ? 1 : hp;
        }

        /// <summary>폰이 예약할 수 있는 비누 한 개비를 찾는다. 없으면 null.</summary>
        public static Thing FindSoap(Pawn pawn)
        {
            ThingDef def = SoapDef;
            if (def == null || pawn.Map == null)
            {
                return null;
            }

            List<Thing> list = pawn.Map.listerThings.ThingsOfDef(def);
            for (int i = 0; i < list.Count; i++)
            {
                Thing soap = list[i];
                if (!soap.Spawned || soap.IsForbidden(pawn))
                {
                    continue;
                }

                if (pawn.CanReserve(soap, 1, -1, null, false))
                {
                    return soap;
                }
            }

            return null;
        }

        /// <summary>비누를 소비한다. 스택이 모자라면 통째로 사라진다.</summary>
        public static void ConsumeSoap(Thing soap, int count)
        {
            if (soap == null || soap.Destroyed || count <= 0)
            {
                return;
            }

            if (soap.stackCount <= count)
            {
                soap.Destroy(DestroyMode.Vanish);
                return;
            }

            soap.SplitOff(count).Destroy(DestroyMode.Vanish);
        }

        /// <summary>세탁기용: 맵에서 비누를 찾아 바로 소비한다(폰 없음).</summary>
        public static bool TryConsumeSoapFromMap(Map map, int count)
        {
            ThingDef def = SoapDef;
            if (map == null || def == null || count <= 0)
            {
                return false;
            }

            List<Thing> list = map.listerThings.ThingsOfDef(def);
            for (int i = 0; i < list.Count; i++)
            {
                Thing soap = list[i];
                if (!soap.Spawned || soap.IsForbidden(Faction.OfPlayer))
                {
                    continue;
                }

                ConsumeSoap(soap, count);
                return true;
            }

            return false;
        }

        /// <summary>소비하지 않고 비누가 있는지만 본다(반 개비 청구가 이번 회차를 덮을 때 사용).</summary>
        public static bool HasUsableSoap(Map map)
        {
            ThingDef def = SoapDef;
            if (map == null || def == null)
            {
                return false;
            }

            List<Thing> list = map.listerThings.ThingsOfDef(def);
            for (int i = 0; i < list.Count; i++)
            {
                Thing soap = list[i];
                if (soap.Spawned && !soap.IsForbidden(Faction.OfPlayer))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 세탁할 옷을 고른다. 1순위는 폰이 지금 입고 있는 자기 옷, 2순위는 near 주변에서
        /// 가장 가까운 바닥·재고의 옷이다. 없으면 null.
        /// </summary>
        /// <summary>세탁기에 넣을 더러운 옷(바닥·재고)을 찾는다.</summary>
        public static Thing FindDirtyGarment(Pawn pawn, IntVec3 near, bool withSoap)
        {
            return FindGarmentToWash(pawn, near, withSoap);
        }

        public static Thing FindGarmentToWash(Pawn pawn, IntVec3 near, bool withSoap)
        {
            Map map = pawn.Map;
            if (map == null)
            {
                return null;
            }

            float threshold = WashThreshold(withSoap);

            // 입고 있는 옷은 절대 세탁 대상이 아니다(갈아입기로 벗은 뒤에 빨래한다).
            List<Thing> loose = map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel);
            Thing best = null;
            int bestDist = int.MaxValue;
            for (int i = 0; i < loose.Count; i++)
            {
                Thing thing = loose[i];
                if (!thing.Spawned)
                {
                    continue;
                }

                ThingComp_ClothingHygiene hygiene = thing.TryGetComp<ThingComp_ClothingHygiene>();
                if (hygiene == null || hygiene.IsWorn || hygiene.Filth < threshold)
                {
                    continue;
                }

                if (thing.IsForbidden(pawn))
                {
                    continue;
                }

                int dist = (thing.PositionHeld - near).LengthHorizontalSquared;
                if (dist >= bestDist)
                {
                    continue;
                }

                if (!pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, pawn.NormalMaxDanger(), 1, -1, null, false))
                {
                    continue;
                }

                best = thing;
                bestDist = dist;
            }

            return best;
        }
    }
}
