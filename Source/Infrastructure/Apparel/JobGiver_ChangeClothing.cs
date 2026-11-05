using System.Collections.Generic;
using ClothingHygiene.Domain;
using RimWorld;
using Verse;
using Verse.AI;

namespace ClothingHygiene.Infrastructure
{
    // 폴더는 Infrastructure/Apparel 이지만 네임스페이스에 Apparel 을 넣지 않는다.
    // RimWorld.Apparel 타입과 이름이 겹쳐 컴파일이 깨진다.
    /// <summary>
    /// 오염된 착용 의복을 같은 슬롯의 깨끗한 예비복으로 갈아입는 job 을 발급한다.
    /// Humanlike 폰의 MainColonistBehaviorCore 정렬기에 들어가며, 순위는 GetPriority 가 정한다
    /// (작업 5.5 위, 식사 9.5·수면 아래 = 5.6).
    /// job giver 는 폰에게 진행 중인 job 이 없을 때만 불리므로 매 틱 훅이 아니다.
    /// 전체 의복 스캔은 250틱에 한 번으로 제한한다(예비복이 없을 때의 반복 스캔 비용 차단).
    /// </summary>
    public class JobGiver_ChangeClothing : ThinkNode_JobGiver
    {
        /// <summary>JobGiver_Work(5.5) 보다 높고, JobGiver_GetFood(9.5)·GetRest 보다 낮다.</summary>
        private const float JobPriority = 5.6f;

        /// <summary>예비복 전체 스캔 간격. 폰마다 thingIDNumber 로 위상을 흩어 같은 틱에 몰리지 않게 한다.</summary>
        private const int ScanInterval = 250;

        /// <summary>예비복 오염 상한. Domain.HygieneValue.CleanThreshold(0.02) 는 "완전히 깨끗함" 기준이라 쓰지 않는다.</summary>
        private const float SpareMaxFilth = 0.2f;

        /// <summary>예비복 내구도 하한(비율).</summary>
        private const float SpareMinHitPointRatio = 0.5f;

        private static readonly List<Thing> tmpApparel = new List<Thing>();

        private static JobDef jobDef;

        public static JobDef JobDef
        {
            get
            {
                if (jobDef == null)
                {
                    jobDef = DefDatabase<JobDef>.GetNamed("CH_ChangeClothing");
                }

                return jobDef;
            }
        }

        /// <summary>
        /// 사고 트리는 job 이 없을 때 매 틱 돈다. 그래서 우선순위 계산은 스캔 틱이 아니면 즉시 0 을 돌려주고
        /// (다른 giver 에게 순서를 넘긴다), 스캔 틱에서만 착용 의복을 훑는다.
        /// </summary>
        public override float GetPriority(Pawn pawn)
        {
            HygieneSettings settings = HygieneMod.Settings;

            if (settings == null || !CanThink(pawn) || !IsScanTick(pawn))
            {
                return 0f;
            }

            return HasDirtyGarment(pawn, settings.ChangeThreshold) ? JobPriority : 0f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            HygieneSettings settings = HygieneMod.Settings;

            if (settings == null || !CanThink(pawn) || !IsScanTick(pawn) || !CanChangeNow(pawn))
            {
                return null;
            }

            float threshold = settings.ChangeThreshold;
            List<Apparel> worn = pawn.apparel.WornApparel;

            for (int i = 0; i < worn.Count; i++)
            {
                Apparel dirty = worn[i];

                if (dirty == null || pawn.apparel.IsLocked(dirty) || !PlayerAllowsDrop(pawn, dirty))
                {
                    continue;
                }

                ThingComp_ClothingHygiene comp = dirty.GetComp<ThingComp_ClothingHygiene>();

                if (comp == null || comp.Filth < threshold)
                {
                    continue;
                }

                Apparel spare = FindSpare(pawn, dirty);

                if (spare != null)
                {
                    return JobMaker.MakeJob(JobDef, spare, dirty);
                }
            }

            // 예비복이 없으면 job 을 만들지 않는다 — 다음 스캔까지 재시도하지 않는다.
            return null;
        }

        /// <summary>폰마다 위상을 흩어 같은 틱에 스캔이 몰리지 않게 한다. 상태 저장 없이 틱+id 로 판정한다.</summary>
        private static bool IsScanTick(Pawn pawn)
        {
            return (Find.TickManager.TicksGame + pawn.thingIDNumber) % ScanInterval == 0;
        }

        /// <summary>우선순위 계산에도 부르는 값싼 필터. 여기서 게임 상태를 오래 들여다보지 않는다.</summary>
        private static bool CanThink(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.RaceProps.Humanlike)
            {
                return false;
            }

            if (pawn.Faction != Faction.OfPlayer || pawn.IsQuestLodger())
            {
                return false;
            }

            return pawn.apparel != null && pawn.apparel.WornApparelCount > 0 && pawn.Map != null;
        }

        /// <summary>실제 job 을 만들기 전에만 보는 필터.</summary>
        private static bool CanChangeNow(Pawn pawn)
        {
            if (pawn.Downed || pawn.Drafted || pawn.InMentalState)
            {
                return false;
            }

            if (RestUtility.InBed(pawn) || !RestUtility.Awake(pawn))
            {
                return false;
            }

            return pawn.mutant == null || !pawn.mutant.Def.disableApparel;
        }

        private static bool HasDirtyGarment(Pawn pawn, float threshold)
        {
            List<Apparel> worn = pawn.apparel.WornApparel;

            for (int i = 0; i < worn.Count; i++)
            {
                ThingComp_ClothingHygiene comp = worn[i].GetComp<ThingComp_ClothingHygiene>();

                if (comp != null && comp.Filth >= threshold)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool PlayerAllowsDrop(Pawn pawn, Apparel apparel)
        {
            return pawn.outfits == null || pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(apparel);
        }

        /// <summary>
        /// 바닥·재고·선반에 있는(스폰된) 의복만 후보로 본다. 가장 가까운 유효 후보를 고른다.
        /// 거리순으로 bestDistSq 를 갱신하며 CanReserveAndReach 는 그보다 가까운 후보에만 부른다(경로 탐색 횟수 제한).
        /// </summary>
        private static Apparel FindSpare(Pawn pawn, Apparel dirty)
        {
            Map map = pawn.Map;
            Apparel best = null;
            int bestDistSq = int.MaxValue;

            tmpApparel.Clear();
            map.listerThings.GetAllThings(in tmpApparel, ThingRequestGroup.Apparel, null, lookInHaulSources: true);

            for (int i = 0; i < tmpApparel.Count; i++)
            {
                Apparel spare = tmpApparel[i] as Apparel;

                if (spare == null || spare == dirty || !spare.Spawned || spare.Wearer != null)
                {
                    continue;
                }

                if (spare.IsForbidden(pawn) || FireUtility.IsBurning(spare) || !spare.Position.InAllowedArea(pawn))
                {
                    continue;
                }

                if (!IsAcceptableSpare(pawn, spare, dirty))
                {
                    continue;
                }

                int distSq = (spare.Position - pawn.Position).LengthHorizontalSquared;

                if (distSq >= bestDistSq)
                {
                    continue;
                }

                if (!pawn.CanReserveAndReach(spare, PathEndMode.ClosestTouch, pawn.NormalMaxDanger()))
                {
                    continue;
                }

                best = spare;
                bestDistSq = distSq;
            }

            tmpApparel.Clear();
            return best;
        }

        private static bool IsAcceptableSpare(Pawn pawn, Apparel spare, Apparel dirty)
        {
            ThingComp_ClothingHygiene comp = spare.GetComp<ThingComp_ClothingHygiene>();
            float filth = comp != null ? comp.Filth : 0f;
            float hitPointRatio = spare.MaxHitPoints > 0 ? (float)spare.HitPoints / spare.MaxHitPoints : 1f;

            if (!GarmentChangeRule.IsCleanSpare(filth, hitPointRatio, SpareMaxFilth, SpareMinHitPointRatio))
            {
                return false;
            }

            // 성별·성장 단계.
            if (!spare.PawnCanWear(pawn))
            {
                return false;
            }

            // 신체 부위(없는 부위를 덮는 옷은 Wear 가 경고만 남기고 실패한다).
            if (!ApparelUtility.HasPartsToWear(pawn, spare.def))
            {
                return false;
            }

            // 바이오코드.
            if (CompBiocodable.IsBiocoded(spare) && !CompBiocodable.IsBiocodedFor(spare, pawn))
            {
                return false;
            }

            return ReplacesOnly(pawn, spare, dirty) && RequirementsKept(pawn, spare, dirty);
        }

        /// <summary>
        /// 이 예비복이 dirty 한 벌만 대체하는가.
        /// dirty 와 겹치지 않으면(같은 슬롯이 아니면) 그냥 한 겹 더 입는 것이므로 대상이 아니다.
        /// 다른 착용복과 겹치면 그 옷까지 벗겨지므로 제외한다.
        /// </summary>
        private static bool ReplacesOnly(Pawn pawn, Apparel spare, Apparel dirty)
        {
            BodyDef body = pawn.RaceProps.body;

            if (ApparelUtility.CanWearTogether(spare.def, dirty.def, body))
            {
                return false;
            }

            List<Apparel> worn = pawn.apparel.WornApparel;

            for (int i = 0; i < worn.Count; i++)
            {
                if (worn[i] == dirty)
                {
                    continue;
                }

                if (!ApparelUtility.CanWearTogether(spare.def, worn[i].def, body))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 계급/직책 apparel 요구를 깨지 않는가. 대상 옷이 지금 어떤 요구를 만족시키고 있다면
        /// 예비복도 같은 요구를 만족시켜야 한다. 요구가 걸리지 않은 폰은 즉시 통과한다.
        /// </summary>
        private static bool RequirementsKept(Pawn pawn, Apparel spare, Apparel dirty)
        {
            List<ApparelRequirement> requirements = pawn.apparel.ActiveRequirementsForReading;

            for (int i = 0; i < requirements.Count; i++)
            {
                ApparelRequirement requirement = requirements[i];

                if (!requirement.ApparelMeetsRequirement(dirty.def, false))
                {
                    continue;
                }

                if (!requirement.ApparelMeetsRequirement(spare.def, false))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
