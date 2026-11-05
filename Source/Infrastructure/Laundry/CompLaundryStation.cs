using RimWorld;
using Verse;

namespace ClothingHygiene.Infrastructure
{
    /// <summary>세탁 거점 컴포넌트. 상태 조회와 비누 소비 누적만 담당하고, 실제 세탁 로직은 LaundryUtility 에 있다.</summary>
    public class CompLaundryStation : ThingComp
    {
        /// <summary>반 개비 단위 비누 소비를 누적하는 나머지. 예: 초음파 세탁기 0.5 → 2회마다 1개비.</summary>
        private float soapCredit;

        public CompProperties_LaundryStation Props
        {
            get { return (CompProperties_LaundryStation)props; }
        }

        /// <summary>cycleTicks &gt; 0 이면 폰 없이 스스로 도는 세탁기다.</summary>
        public bool IsMachine
        {
            get { return Props.cycleTicks > 0; }
        }

        /// <summary>전력이 필요한 건물이면 켜져 있어야 한다. 전력 comp 가 없으면 항상 true.</summary>
        public bool Powered
        {
            get
            {
                CompPowerTrader power = parent.GetComp<CompPowerTrader>();
                return power == null || power.PowerOn;
            }
        }

        /// <summary>이번 세탁에 지불해야 할 비누 개비 수. 0 이면 이번에는 지불할 것이 없다.</summary>
        public int ClaimSoap()
        {
            if (Props.soapUnits <= 0f)
            {
                return 0;
            }

            soapCredit += Props.soapUnits;
            if (soapCredit < 1f)
            {
                return 0;
            }

            int bars = (int)soapCredit;
            soapCredit -= bars;
            return bars;
        }

        /// <summary>비누를 실제로 지급하지 못했을 때 청구를 되돌린다.</summary>
        public void CancelSoapClaim(int bars)
        {
            soapCredit += bars - Props.soapUnits;
            if (soapCredit < 0f)
            {
                soapCredit = 0f;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref soapCredit, "ch_soapCredit", 0f);
        }

        public override string CompInspectStringExtra()
        {
            if (!IsMachine)
            {
                return null;
            }

            if (!Powered)
            {
                return "CH_Laundry_Status".Translate("CH_Laundry_NoPower".Translate());
            }

            if (!LaundryUtility.WaterAvailable())
            {
                return "CH_Laundry_Status".Translate("CH_Laundry_NeedWater".Translate());
            }

            return "CH_Laundry_Status".Translate("CH_Laundry_Ready".Translate());
        }
    }
}
