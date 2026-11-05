using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ClothingHygiene.Infrastructure
{
    /// <summary>더러운 옷을 세탁기 안으로 넣는다. targetA = 세탁기, targetB = 옷.</summary>
    public class JobDriver_LoadWasher : JobDriver
    {
        private const TargetIndex WasherInd = TargetIndex.A;
        private const TargetIndex GarmentInd = TargetIndex.B;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.GetTarget(WasherInd), job, 1, -1, null, errorOnFailed))
            {
                return false;
            }

            return pawn.Reserve(job.GetTarget(GarmentInd), job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(WasherInd);
            this.FailOnDestroyedOrNull(GarmentInd);

            yield return Toils_Goto.GotoThing(GarmentInd, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(GarmentInd, false, false, false, false);
            yield return Toils_Goto.GotoThing(WasherInd, PathEndMode.Touch);

            Toil load = ToilMaker.MakeToil("LoadWasher");
            load.initAction = delegate
            {
                Building_LaundryMachine washer = job.GetTarget(WasherInd).Thing as Building_LaundryMachine;
                Thing carried = pawn.carryTracker.CarriedThing;

                if (washer == null || carried == null)
                {
                    return;
                }

                Thing dropped;
                pawn.carryTracker.TryDropCarriedThing(pawn.PositionHeld, ThingPlaceMode.Near, out dropped);

                if (dropped != null)
                {
                    washer.TryAdd(dropped);
                }
            };
            load.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return load;
        }
    }
}
