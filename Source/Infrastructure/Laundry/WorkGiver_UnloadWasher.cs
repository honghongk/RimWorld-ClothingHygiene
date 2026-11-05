using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ClothingHygiene.Infrastructure
{
    /// <summary>세탁기에서 다 씻긴 옷을 꺼내 발밑에 내려놓는다(이후는 바닐라 운반이 처리).</summary>
    public class WorkGiver_UnloadWasher : WorkGiver_Scanner
    {
        private static JobDef cachedJobDef;

        public override PathEndMode PathEndMode
        {
            get { return PathEndMode.Touch; }
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.listerBuildings.allBuildingsColonist;
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return JobDefOfUnload == null;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_LaundryMachine washer = t as Building_LaundryMachine;

            if (washer == null || !washer.HasCleanGarment || JobDefOfUnload == null)
            {
                return false;
            }

            return pawn.CanReserveAndReach(t, PathEndMode.Touch, pawn.NormalMaxDanger(), 1, -1, null, forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!HasJobOnThing(pawn, t, forced))
            {
                return null;
            }

            return JobMaker.MakeJob(JobDefOfUnload, t);
        }

        private static JobDef JobDefOfUnload
        {
            get
            {
                if (cachedJobDef == null)
                {
                    cachedJobDef = DefDatabase<JobDef>.GetNamed("CH_UnloadWasher", false);
                }

                return cachedJobDef;
            }
        }
    }
}
