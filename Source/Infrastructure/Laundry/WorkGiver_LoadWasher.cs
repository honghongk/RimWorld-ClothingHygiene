using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ClothingHygiene.Infrastructure
{
    /// <summary>
    /// 세탁기에 더러운 옷을 넣는 운반 작업(Hauling). 옷을 집어 세탁기 안으로 넣는다.
    /// </summary>
    public class WorkGiver_LoadWasher : WorkGiver_Scanner
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
            return JobDefOfLoad == null;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_LaundryMachine washer = t as Building_LaundryMachine;

            if (washer == null || !washer.HasSpace || JobDefOfLoad == null)
            {
                return false;
            }

            if (washer.GetComp<CompPowerTrader>() != null && !washer.GetComp<CompPowerTrader>().PowerOn)
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

            Thing garment = LaundryUtility.FindDirtyGarment(pawn, t.Position, LaundryUtility.HasUsableSoap(pawn.Map));

            if (garment == null)
            {
                return null;
            }

            Job job = JobMaker.MakeJob(JobDefOfLoad, t, garment);
            job.count = 1;
            return job;
        }

        private static JobDef JobDefOfLoad
        {
            get
            {
                if (cachedJobDef == null)
                {
                    cachedJobDef = DefDatabase<JobDef>.GetNamed("CH_LoadWasher", false);
                }

                return cachedJobDef;
            }
        }
    }
}
