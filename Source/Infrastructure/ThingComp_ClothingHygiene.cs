using ClothingHygiene.Domain;
using ClothingHygiene.Presentation;
using RimWorld;
using Verse;

namespace ClothingHygiene.Infrastructure
{
    /// <summary>
    /// 의복 한 벌의 오염도. 값은 그 아이템의 세이브 노드에 저장되므로, 옷을 벗어 바닥·재고에 두거나
    /// 다른 림에게 넘겨도 오염도가 따라간다. 슬롯·폰 단위 캐싱 금지.
    /// </summary>
    public class ThingComp_ClothingHygiene : ThingComp, IHygieneState
    {
        public float filth;

        public CompProperties_ClothingHygiene Props
        {
            get { return (CompProperties_ClothingHygiene)props; }
        }

        public float Filth
        {
            get { return filth; }
            set { filth = HygieneValue.Clamp(value); }
        }

        public bool IsWorn
        {
            get
            {
                Apparel apparel = parent as Apparel;
                return apparel != null && apparel.Wearer != null;
            }
        }

        public float GarmentFactor
        {
            get { return Props.filthFactor; }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref filth, "ch_filth", 0f);
            filth = HygieneValue.Clamp(filth);
        }

        public override string CompInspectStringExtra()
        {
            return HygieneInspectText.Format(filth);
        }

        public override string CompTipStringExtra()
        {
            return HygieneInspectText.FormatShort(filth);
        }

        public override string GetDescriptionPart()
        {
            return HygieneInspectText.Describe(filth);
        }
    }
}
