using ClothingHygiene.Domain;

namespace ClothingHygiene.Infrastructure.Compat
{
    /// <summary>물 없이 세탁하는 기본 구현. DBH 미설치 또는 NoWater 설정일 때 쓴다.</summary>
    public class NoWaterSource : IWaterSource
    {
        public bool Available
        {
            get { return true; }
        }

        public string LabelKey
        {
            get { return "CH_Water_None"; }
        }

        public bool TryConsume(float amount)
        {
            return true;
        }
    }
}
