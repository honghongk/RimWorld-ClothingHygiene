using System.Collections.Generic;
using ClothingHygiene.Domain;
using RimWorld;
using Verse;

namespace ClothingHygiene.Infrastructure
{
    /// <summary>
    /// 의복 재질(stuff)로 오염 배율을 정한다. 금속 갑옷은 거의 안 더러워지고 천은 빨리 더러워진다.
    ///
    /// 판정 순서는 바닐라 stuff 카테고리(실측: Metallic·Stony·Woody·Leathery·Fabric) 기준이며,
    /// 모드 재질도 카테고리만 갖고 있으면 자동으로 따라온다. 재질이 없는 의복(무재질 def)은 1.0.
    /// </summary>
    public static class MaterialFilthFactor
    {
        public static float Of(Thing thing)
        {
            if (thing == null || thing.Stuff == null)
            {
                return 1f;
            }

            StuffProperties props = thing.Stuff.stuffProps;

            if (props == null || props.categories == null)
            {
                return 1f;
            }

            List<StuffCategoryDef> categories = props.categories;

            for (int i = 0; i < categories.Count; i++)
            {
                GarmentMaterial material = KindOf(categories[i]);

                if (material != GarmentMaterial.Other)
                {
                    return FilthRates.Material(material);
                }
            }

            return 1f;
        }

        private static GarmentMaterial KindOf(StuffCategoryDef category)
        {
            if (category == null)
            {
                return GarmentMaterial.Other;
            }

            if (category == StuffCategoryDefOf.Metallic)
            {
                return GarmentMaterial.Metal;
            }

            if (category == StuffCategoryDefOf.Stony)
            {
                return GarmentMaterial.Stone;
            }

            if (category == StuffCategoryDefOf.Woody)
            {
                return GarmentMaterial.Wood;
            }

            if (category == StuffCategoryDefOf.Leathery)
            {
                return GarmentMaterial.Leather;
            }

            if (category == StuffCategoryDefOf.Fabric)
            {
                return GarmentMaterial.Cloth;
            }

            return GarmentMaterial.Other;
        }
    }
}
