using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ClothingHygiene.Infrastructure
{
    /// <summary>
    /// 세탁 작업을 붙잡는다. 작업 대상은 세탁대(수동)이며, 실제로 씻을 옷은
    /// 작업을 만들 때 한 번만 탐색해 targetB 로 넘긴다. 세탁기는 자동이므로 대상이 아니다.
    /// </summary>
    public class WorkGiver_WashClothing : WorkGiver_Scanner
    {
        private static JobDef cachedJobDef;

        // 기본값(Undefined)을 그대로 쓴다. PotentialWorkThingsGlobal 로 후보를 직접 주므로
        // 지역(region) 선검색을 건너뛰고 우리 목록만 훑는다(젠클로스트 global 경로).
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
            return !LaundryUtility.WaterAvailable() || JobDefOfWash == null;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            CompLaundryStation station = StationOf(t);
            if (station == null || station.IsMachine || !station.Powered)
            {
                return false;
            }

            if (JobDefOfWash == null)
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

            Thing soap = LaundryUtility.FindSoap(pawn);
            Thing garment = LaundryUtility.FindGarmentToWash(pawn, t.Position, soap != null);
            if (garment == null)
            {
                return null;
            }

            Job job = JobMaker.MakeJob(JobDefOfWash, t, garment);
            job.count = 1;

            if (soap != null)
            {
                job.targetC = soap;
            }

            return job;
        }

        private static CompLaundryStation StationOf(Thing t)
        {
            ThingWithComps withComps = t as ThingWithComps;
            return withComps != null ? withComps.TryGetComp<CompLaundryStation>() : null;
        }

        private static JobDef JobDefOfWash
        {
            get
            {
                if (cachedJobDef == null)
                {
                    cachedJobDef = DefDatabase<JobDef>.GetNamed("CH_WashClothing", false);
                }

                return cachedJobDef;
            }
        }
    }
}
