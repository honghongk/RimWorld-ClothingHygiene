using Verse;

namespace ClothingHygiene.Infrastructure
{
    /// <summary>
    /// 물가 세탁 자리: 8방향 중 한 칸이라도 물이어야 설치할 수 있다.
    /// </summary>
    public class PlaceWorker_WaterEdge : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0)
                    {
                        continue;
                    }

                    IntVec3 cell = loc + new IntVec3(dx, 0, dz);

                    if (cell.InBounds(map) && map.terrainGrid.TerrainAt(cell).IsWater)
                    {
                        return AcceptanceReport.WasAccepted;
                    }
                }
            }

            return new AcceptanceReport("CH_NeedWaterAdjacent".Translate());
        }
    }
}
