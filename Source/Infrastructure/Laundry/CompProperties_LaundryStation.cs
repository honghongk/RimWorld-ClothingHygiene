using Verse;

namespace ClothingHygiene.Infrastructure
{
    /// <summary>
    /// 세탁 거점(손빨래통·세탁통·세탁기)의 공통 수치. 값은 ThingDef XML 에서 채운다.
    /// </summary>
    public class CompProperties_LaundryStation : CompProperties
    {
        /// <summary>손세탁 1회 작업량. 소요 틱 = workAmount / WorkSpeedGlobal.</summary>
        public float workAmount = 1200f;

        /// <summary>세탁 1회 물 소비량(IWaterSource 단위).</summary>
        public float waterUnits = 1f;

        /// <summary>세탁 1회 비누 소비량(개비). 0.5 처럼 반 개비 단위는 세탁대에 누적된다.</summary>
        public float soapUnits = 1f;

        /// <summary>0 이면 수동 세탁대. 0 보다 크면 자동 세탁기이며 값은 세탁 주기(틱).</summary>
        public int cycleTicks;

        /// <summary>자동 세탁기에 넣을 수 있는 옷 수납 칸 수.</summary>
        public int capacity = 5;

        /// <summary>세탁기가 가동을 시작하기 전 대기 시간(틱). 이 동안은 옷을 더 넣을 수 있다.</summary>
        public int startDelayTicks = 900;

        public CompProperties_LaundryStation()
        {
            compClass = typeof(CompLaundryStation);
        }
    }
}
