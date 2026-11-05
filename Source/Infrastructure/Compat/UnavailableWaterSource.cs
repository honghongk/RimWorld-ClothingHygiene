using ClothingHygiene.Domain;

namespace ClothingHygiene.Infrastructure.Compat
{
    /// <summary>
    /// RequireWater 설정인데 물 공급원이 없을 때(DBH 미설치·리플렉션 실패) 쓰는 구현.
    /// 항상 Available=false 라서 세탁이 막힌다 — 물이 없다는 사실을 숨기지 않는다.
    /// </summary>
    public class UnavailableWaterSource : IWaterSource
    {
        public bool Available
        {
            get { return false; }
        }

        public string LabelKey
        {
            get { return "CH_Water_Missing"; }
        }

        public bool TryConsume(float amount)
        {
            return false;
        }
    }
}
