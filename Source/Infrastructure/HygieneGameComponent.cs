using System.Collections.Generic;
using ClothingHygiene.Application;
using ClothingHygiene.Domain;
using ClothingHygiene.Infrastructure.Performance;
using RimWorld;
using Verse;

namespace ClothingHygiene.Infrastructure
{
    /// <summary>
    /// 오염 누적 진입점. 의복은 tickerType=Never 라 ThingComp.CompTick 이 불리지 않으므로
    /// GameComponent 가 250틱마다 직접 순회한다. GameComponent 는 ctor(Game) 만 있으면 자동 등록된다.
    /// </summary>
    public class HygieneGameComponent : GameComponent
    {
        public const int Interval = 250;

        public HygieneGameComponent(Game game)
        {
        }

        public override void GameComponentTick()
        {
            HygieneSettings settings = HygieneMod.Settings;
            int tick = Find.TickManager.TicksGame;

            if (settings == null || tick % Interval != 0)
            {
                return;
            }

            HygieneProfiler.Begin();

            try
            {
                List<Map> maps = Find.Maps;

                for (int i = 0; i < maps.Count; i++)
                {
                    Map map = maps[i];
                    float temperature = map.mapTemperature.OutdoorTemp;
                    IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;

                    for (int j = 0; j < pawns.Count; j++)
                    {
                        Pawn pawn = pawns[j];

                        if (!pawn.RaceProps.Humanlike || pawn.apparel == null || pawn.Dead)
                        {
                            continue;
                        }

                        ActivityKind activity = ActivityReader.Read(pawn);
                        List<Apparel> worn = pawn.apparel.WornApparel;

                        for (int k = 0; k < worn.Count; k++)
                        {
                            ThingComp_ClothingHygiene comp = worn[k].GetComp<ThingComp_ClothingHygiene>();

                            if (comp != null)
                            {
                                HygieneTickUseCase.AccumulateOne(comp, settings.filthPerDay, Interval, activity, temperature);
                            }
                        }
                    }
                }
            }
            finally
            {
                HygieneProfiler.End(tick);
            }
        }

        public override void ExposeData()
        {
        }
    }
}
