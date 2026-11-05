using System.Collections.Generic;
using ClothingHygiene.Infrastructure;
using RimWorld;
using Verse;

namespace ClothingHygiene.Infrastructure
{
    /// <summary>
    /// 모든 의복 ThingDef 에 오염 comp 를 붙인다.
    ///
    /// XML 패치(ApparelNoQualityBase)만으로는 그 추상 부모를 상속하지 않는 모드 의복에 comp 이 없다.
    /// 여기서 def 로딩 후 한 번 훑어 빠진 것만 채운다(이미 있으면 건드리지 않으므로 중복 comp 없음).
    /// DefDatabase 는 상속 해석이 끝난 뒤라 abstract def 는 들어 있지 않다 — 실 ThingDef 만 대상이다.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class ApparelCompInjector
    {
        static ApparelCompInjector()
        {
            List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading;
            int added = 0;

            for (int i = 0; i < defs.Count; i++)
            {
                ThingDef def = defs[i];

                if (def.thingClass == null || !typeof(Apparel).IsAssignableFrom(def.thingClass))
                {
                    continue;
                }

                if (def.comps == null || HasHygieneComp(def.comps))
                {
                    continue;
                }

                def.comps.Add(new CompProperties_ClothingHygiene());
                added++;
            }

            if (added > 0)
            {
                Log.Message("[ClothingHygiene] fouling comp attached to " + added + " apparel def(s) via runtime injection");
            }
        }

        private static bool HasHygieneComp(List<CompProperties> comps)
        {
            for (int i = 0; i < comps.Count; i++)
            {
                if (comps[i] is CompProperties_ClothingHygiene)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
