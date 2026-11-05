using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ClothingHygiene.Infrastructure.Patches
{
    /// <summary>
    /// 의복 아이콘에 청결도 막대를 그린다.
    ///
    /// 후킹 지점은 `Widgets.ThingIcon(Rect, Thing, ...)` — 바닐라 장비 탭·재고·상점이 모두 이 함수로
    /// 아이콘을 그리므로, UI 를 갈아엎은 모드도 이 함수를 부르면 자동으로 같이 표시된다.
    ///
    /// 조건: 착용 중인 의복(Wearer != null) + 오염도가 깨끗 임계 이상일 때만. 좌측에 세로 막대.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class ApparelIconOverlay
    {
        private const float BarWidthRatio = 0.14f;
        private const float MinBarWidth = 2f;

        private static readonly Color StageClean = new Color(0.35f, 0.85f, 0.35f);
        private static readonly Color StageWorn = new Color(0.75f, 0.85f, 0.25f);
        private static readonly Color StageSmelly = new Color(0.95f, 0.75f, 0.2f);
        private static readonly Color StageClammy = new Color(0.95f, 0.5f, 0.15f);
        private static readonly Color StageFilthy = new Color(0.9f, 0.2f, 0.15f);

        static ApparelIconOverlay()
        {
            MethodInfo target = AccessTools.Method(
                typeof(Widgets),
                nameof(Widgets.ThingIcon),
                new[] { typeof(Rect), typeof(Thing), typeof(float), typeof(Rot4?), typeof(bool), typeof(float), typeof(bool) });

            if (target == null)
            {
                Log.Warning("[ClothingHygiene] Widgets.ThingIcon signature not found — apparel filth bar disabled");
                return;
            }

            new Harmony("honghongk.clothinghygiene.iconbar").Patch(
                target,
                postfix: new HarmonyMethod(typeof(ApparelIconOverlay), nameof(Postfix)));
        }

        public static void Postfix(Rect rect, Thing thing)
        {
            Apparel apparel = thing as Apparel;

            if (apparel == null || apparel.Wearer == null)
            {
                return;
            }

            ThingComp_ClothingHygiene comp = apparel.GetComp<ThingComp_ClothingHygiene>();

            if (comp == null || comp.Filth < Domain.HygieneValue.CleanThreshold)
            {
                return;
            }

            float width = rect.width * BarWidthRatio;

            if (width < MinBarWidth)
            {
                width = MinBarWidth;
            }

            Rect bar = new Rect(rect.x + 1f, rect.y + 1f, width, rect.height - 2f);
            Widgets.DrawBoxSolid(bar, ColorOf(comp.Filth));
        }

        private static Color ColorOf(float filth)
        {
            switch (Domain.HygieneStageInfo.Of(filth))
            {
                case Domain.HygieneStage.Worn:
                    return StageWorn;
                case Domain.HygieneStage.Smelly:
                    return StageSmelly;
                case Domain.HygieneStage.Clammy:
                    return StageClammy;
                case Domain.HygieneStage.Filthy:
                    return StageFilthy;
                default:
                    return StageClean;
            }
        }
    }
}
