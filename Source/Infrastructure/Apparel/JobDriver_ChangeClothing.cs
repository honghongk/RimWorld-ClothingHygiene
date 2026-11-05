using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ClothingHygiene.Infrastructure
{
    // 폴더는 Infrastructure/Apparel 이지만 네임스페이스에 Apparel 을 넣지 않는다(RimWorld.Apparel 과 충돌).
    /// <summary>
    /// 예비복 위치로 걸어가 갈아입는다. job.targetA = 입을 예비복, job.targetB = 벗겨질 착용 의복.
    /// 벗은 옷은 Pawn_ApparelTracker.Wear(dropReplacedApparel:true) 가 바닥에 떨어뜨린다.
    /// </summary>
    public class JobDriver_ChangeClothing : JobDriver
    {
        private const TargetIndex SpareInd = TargetIndex.A;
        private const TargetIndex DirtyInd = TargetIndex.B;

        private const int FallbackWearTicks = 60;

        private Apparel Spare
        {
            get { return job.GetTarget(SpareInd).Thing as Apparel; }
        }

        private Apparel Dirty
        {
            get { return job.GetTarget(DirtyInd).Thing as Apparel; }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        public override string GetReport()
        {
            Apparel spare = Spare;

            if (spare == null)
            {
                return "CH_JobReport_ChangeClothing_Generic".Translate();
            }

            return "CH_JobReport_ChangeClothing".Translate(spare.LabelShort);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(SpareInd, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(SpareInd);

            Toil wait = Toils_General.Wait(WearTicks(), SpareInd);
            wait.FailOnDespawnedNullOrForbidden(SpareInd);
            yield return wait;

            yield return Toils_General.Do(WearSpare);
        }

        private int WearTicks()
        {
            Apparel spare = Spare;

            if (spare == null)
            {
                return FallbackWearTicks;
            }

            int ticks = (int)(spare.GetStatValue(StatDefOf.EquipDelay) * 60f);
            return ticks < 1 ? 1 : ticks;
        }

        private void WearSpare()
        {
            Apparel spare = Spare;

            if (spare == null || !spare.Spawned)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            if (!spare.PawnCanWear(pawn) || !ApparelUtility.HasPartsToWear(pawn, spare.def))
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            Apparel dirty = Dirty;

            // dropReplacedApparel:true → 같은 슬롯의 착용복이 바닥으로 떨어진다.
            // 플레이어 소속 폰이면 게임이 forbid=false 로 떨어뜨리지만, 명시적으로 한 번 더 확인한다.
            pawn.apparel.Wear(spare, dropReplacedApparel: true, locked: false);

            if (dirty != null && dirty.Spawned)
            {
                dirty.SetForbidden(false, false);
            }
        }
    }
}
