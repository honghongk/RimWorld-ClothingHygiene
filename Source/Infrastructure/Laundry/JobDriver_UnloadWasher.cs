using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ClothingHygiene.Infrastructure
{
    /// <summary>세탁기에서 다 씻긴 옷 한 벌을 꺼내 발밑에 놓는다. targetA = 세탁기.</summary>
    public class JobDriver_UnloadWasher : JobDriver
    {
        private const TargetIndex WasherInd = TargetIndex.A;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(WasherInd), job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(WasherInd);

            yield return Toils_Goto.GotoThing(WasherInd, PathEndMode.Touch);

            Toil take = ToilMaker.MakeToil("UnloadWasher");
            take.initAction = delegate
            {
                Building_LaundryMachine washer = job.GetTarget(WasherInd).Thing as Building_LaundryMachine;

                if (washer == null)
                {
                    return;
                }

                Thing clean = washer.TakeFirstClean();

                if (clean == null)
                {
                    return;
                }

                if (!pawn.carryTracker.TryStartCarry(clean))
                {
                    GenPlace.TryPlaceThing(clean, pawn.PositionHeld, pawn.Map, ThingPlaceMode.Near);
                }
            };
            take.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return take;

            Toil drop = ToilMaker.MakeToil("DropWashed");
            drop.initAction = delegate
            {
                if (pawn.carryTracker.CarriedThing == null)
                {
                    return;
                }

                Thing dropped;
                pawn.carryTracker.TryDropCarriedThing(pawn.PositionHeld, ThingPlaceMode.Near, out dropped);
            };
            drop.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return drop;
        }
    }
}
