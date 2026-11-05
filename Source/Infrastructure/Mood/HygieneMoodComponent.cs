using System.Collections.Generic;
using ClothingHygiene.Domain;
using RimWorld;
using Verse;

namespace ClothingHygiene.Infrastructure.Mood
{
    /// <summary>
    /// 의복 청결도 → 무드. 250틱마다 전 맵을 순회하며 <b>우리 모드 thought 4종만</b> 지우고 다시 부여한다.
    /// 다른 모드·바닐라 thought 는 건드리지 않는다.
    ///
    /// 보너스(CH_Clean)는 깨끗한 벌 수만큼 중첩(상한 cleanBonusCap, def 의 stackLimit 와 동일한 5).
    /// 디버프는 가장 더러운 한 벌의 단계 하나만 — 상호 배타.
    ///
    /// 배율(moodPenaltyScale)은 ThoughtDef 수치를 건드리지 않고, 부여하는 Thought_Memory 의
    /// moodPowerFactor(public float, 세이브됨)에 곱한다. Thought_Memory.MoodOffset() 이
    /// baseMoodEffect * moodPowerFactor 로 계산하므로 수치가 그대로 배율 적용된다.
    /// </summary>
    public class HygieneMoodComponent : GameComponent
    {
        public const int Interval = 250;

        /// <summary>CH_Clean 의 ThoughtDef.stackLimit 과 같아야 한다. 넘겨 부여하면 게임이 오래된 기억을 합쳐 상한이 깨진다.</summary>
        public const int MaxCleanStacks = 5;

        private const string CleanDefName = "CH_Clean";
        private const string SmellyDefName = "CH_Smelly";
        private const string ClammyDefName = "CH_Clammy";
        private const string FilthyDefName = "CH_Filthy";

        public HygieneMoodComponent(Game game)
        {
        }

        /// <summary>새 게임 직후 첫 250틱을 기다리지 않고 바로 반영한다(시작하자마자 +1 이 보이도록).</summary>
        public override void StartedNewGame()
        {
            RunPass();
        }

        public override void LoadedGame()
        {
            RunPass();
        }

        public override void GameComponentTick()
        {
            HygieneSettings tickSettings = HygieneMod.Settings;

            if (tickSettings == null || !tickSettings.moodEnabled)
            {
                return;
            }

            if (Find.TickManager.TicksGame % Interval != 0)
            {
                return;
            }

            RunPass();
        }

        private static void RunPass()
        {
            HygieneSettings settings = HygieneMod.Settings;

            if (settings == null || !settings.moodEnabled)
            {
                return;
            }

            // def 조회는 주기당 4회. def 재로딩(언어 변경 등)에도 안전하도록 매 주기 다시 찾는다.
            ThoughtDef clean = DefDatabase<ThoughtDef>.GetNamedSilentFail(CleanDefName);
            ThoughtDef smelly = DefDatabase<ThoughtDef>.GetNamedSilentFail(SmellyDefName);
            ThoughtDef clammy = DefDatabase<ThoughtDef>.GetNamedSilentFail(ClammyDefName);
            ThoughtDef filthy = DefDatabase<ThoughtDef>.GetNamedSilentFail(FilthyDefName);

            if (clean == null || smelly == null || clammy == null || filthy == null)
            {
                return;
            }

            int cap = settings.cleanBonusCap;

            if (cap > MaxCleanStacks)
            {
                cap = MaxCleanStacks;
            }

            if (cap < 0)
            {
                cap = 0;
            }

            float scale = settings.moodPenaltyScale;

            if (scale < 0f)
            {
                scale = 0f;
            }

            List<Map> maps = Find.Maps;

            for (int i = 0; i < maps.Count; i++)
            {
                IReadOnlyList<Pawn> pawns = maps[i].mapPawns.AllPawnsSpawned;

                for (int j = 0; j < pawns.Count; j++)
                {
                    ApplyTo(pawns[j], clean, smelly, clammy, filthy, cap, scale);
                }
            }
        }

        private static void ApplyTo(Pawn pawn, ThoughtDef clean, ThoughtDef smelly, ThoughtDef clammy, ThoughtDef filthy, int cap, float scale)
        {
            if (pawn == null || pawn.Dead || !pawn.RaceProps.Humanlike)
            {
                return;
            }

            if (pawn.apparel == null || pawn.story == null || pawn.needs == null || pawn.needs.mood == null)
            {
                return;
            }

            List<Apparel> worn = pawn.apparel.WornApparel;
            int cleanCount = 0;
            HygieneStage dirtiest = HygieneStage.Clean;

            for (int i = 0; i < worn.Count; i++)
            {
                ThingComp_ClothingHygiene comp = worn[i].GetComp<ThingComp_ClothingHygiene>();

                if (comp == null)
                {
                    continue;
                }

                HygieneStage stage = HygieneStageInfo.Of(comp.Filth);

                if (stage == HygieneStage.Clean)
                {
                    // 상한까지만 센다 — 어차피 그 이상은 부여하지 않는다.
                    if (cleanCount < cap)
                    {
                        cleanCount++;
                    }
                }
                else if (stage > dirtiest)
                {
                    dirtiest = stage;
                }
            }

            MemoryThoughtHandler memories = pawn.needs.mood.thoughts.memories;

            RemoveOurThoughts(memories, clean, smelly, clammy, filthy);

            for (int i = 0; i < cleanCount; i++)
            {
                Grant(memories, clean, 1f);
            }

            ThoughtDef debuff = DebuffFor(dirtiest, smelly, clammy, filthy);

            if (debuff != null && scale > 0f)
            {
                Grant(memories, debuff, scale);
            }
        }

        /// <summary>가장 더러운 단계 → 디버프 def. Worn·Clean 은 디버프 없음.</summary>
        private static ThoughtDef DebuffFor(HygieneStage stage, ThoughtDef smelly, ThoughtDef clammy, ThoughtDef filthy)
        {
            switch (stage)
            {
                case HygieneStage.Smelly:
                    return smelly;
                case HygieneStage.Clammy:
                    return clammy;
                case HygieneStage.Filthy:
                    return filthy;
                default:
                    return null;
            }
        }

        /// <summary>우리 def 4종의 기억만 제거한다. 다른 def 는 비교조차 하지 않고 지나간다.</summary>
        private static void RemoveOurThoughts(MemoryThoughtHandler memories, ThoughtDef a, ThoughtDef b, ThoughtDef c, ThoughtDef d)
        {
            List<Thought_Memory> list = memories.Memories;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                ThoughtDef def = list[i].def;

                if (def == a || def == b || def == c || def == d)
                {
                    memories.RemoveMemory(list[i]);
                }
            }
        }

        /// <summary>기억 1개 부여. moodPowerFactor 가 Thought_Memory.MoodOffset() 에서 곱해진다.</summary>
        private static void Grant(MemoryThoughtHandler memories, ThoughtDef def, float moodPowerFactor)
        {
            Thought_Memory memory = ThoughtMaker.MakeThought(def, (Precept)null);
            memory.moodPowerFactor = moodPowerFactor;
            memories.TryGainMemory(memory);
        }
    }
}
