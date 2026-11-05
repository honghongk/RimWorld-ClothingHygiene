using System;
using ClothingHygiene.Domain;
using Verse;
using Verse.AI;

namespace ClothingHygiene.Infrastructure
{
    /// <summary>
    /// 폰이 지금 얼마나 땀을 흘리는지 판정한다.
    ///
    /// 직업 이름 전수 매칭 대신 4단 신호를 쓴다(실측 API 기반):
    ///   ① 수면(`jobs.curDriver.asleep`)                   → Sleep
    ///   ② 실제 격한 동작 — 근접 공격·채굴·화재 진압        → Strenuous
    ///   ③ 원거리 사격(서서 쏘는 Attack/UseVerb 계열)       → Idle   ← 총 쏘는 건 땀이 적다
    ///   ④ 대기·배회·소집(`JobDef.isIdle`)                  → Idle   ← 소집은 전투가 아니다
    ///      이동 중이거나 WorkGiver 가 붙은 작업             → Labor
    ///
    /// 결과: "사냥"은 동물을 찾아 걸어다니는 시간만 노동으로 잡히고, 서서 조준·사격하는 시간은 평상으로 떨어진다.
    /// </summary>
    public static class ActivityReader
    {
        /// <summary>몸을 실제로 쓰는 동작만. 사냥·원거리 사격은 넣지 않는다.</summary>
        private static readonly string[] HeavyKeywords =
        {
            "AttackMelee", "MeleeAttack", "BeatFire", "Extinguish", "Mine"
        };

        public static ActivityKind Read(Pawn pawn)
        {
            if (pawn == null || pawn.jobs == null)
            {
                return ActivityKind.Idle;
            }

            JobDriver driver = pawn.jobs.curDriver;
            if (driver != null && driver.asleep)
            {
                return ActivityKind.Sleep;
            }

            Job job = pawn.jobs.curJob;
            JobDef def = job != null ? job.def : null;

            if (def == null)
            {
                return ActivityKind.Idle;
            }

            if (ContainsAny(def.defName, HeavyKeywords))
            {
                return ActivityKind.Strenuous;
            }

            // 원거리 사격: 서서 쏘는 동안은 거의 안 더러워진다(사냥 포함).
            if (def.defName.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ActivityKind.Idle;
            }

            if (def.isIdle)
            {
                return ActivityKind.Idle;
            }

            if (pawn.pather != null && pawn.pather.Moving)
            {
                return ActivityKind.Labor;
            }

            if (job.workGiverDef != null)
            {
                return ActivityKind.Labor;
            }

            return ActivityKind.Idle;
        }

        private static bool ContainsAny(string name, string[] keywords)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                if (name.IndexOf(keywords[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
