using ClothingHygiene.Domain;
using Verse;

namespace ClothingHygiene.Presentation
{
    /// <summary>
    /// 의복 청결도 표시. 항상 표시한다(깨끗한 옷도 0% 로 보여야 상태를 확인할 수 있다).
    /// 틱 루프에서 부르지 않는다 — inspect/툴팁/정보카드에서만.
    /// </summary>
    public static class HygieneInspectText
    {
        public static string Format(float filth)
        {
            HygieneStage stage = HygieneStageInfo.Of(filth);
            return "CH_InspectString".Translate(HygieneStageInfo.LabelKeyOf(stage).Translate(), filth.ToStringPercent());
        }

        /// <summary>호버 툴팁용 짧은 한 줄(Thing.GetTooltip 이 CompTipStringExtra 를 읽는다).</summary>
        public static string FormatShort(float filth)
        {
            HygieneStage stage = HygieneStageInfo.Of(filth);
            return "CH_TipString".Translate(HygieneStageInfo.LabelKeyOf(stage).Translate(), filth.ToStringPercent());
        }

        /// <summary>정보 카드(i) 본문.</summary>
        public static string Describe(float filth)
        {
            return "CH_DescriptionPart".Translate(filth.ToStringPercent());
        }
    }
}
