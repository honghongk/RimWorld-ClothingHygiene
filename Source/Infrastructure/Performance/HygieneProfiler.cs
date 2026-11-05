using System;
using System.Diagnostics;
using System.Globalization;
using Verse;

namespace ClothingHygiene.Infrastructure.Performance
{
    /// <summary>
    /// 250틱 순회 구간 계측. 항상 켜져 있지만 로그는 게임 내 하루(60000틱)에 1줄만 남긴다.
    /// 틱당 비용은 Stopwatch 시작/정지 + long 누적뿐 — 문자열/할당 없음.
    /// </summary>
    public static class HygieneProfiler
    {
        /// <summary>게임 내 하루 = 60000틱.</summary>
        public const int TicksPerDay = 60000;

        private static readonly Stopwatch Watch = new Stopwatch();

        private static long totalRawTicks;
        private static long maxRawTicks;
        private static int passes;
        private static int windowStartTick = -1;

        public static void Begin()
        {
            Watch.Reset();
            Watch.Start();
        }

        /// <summary>Begin 이후 경과를 누적하고, 하루가 지났으면 1줄 로그 후 창을 리셋한다.</summary>
        public static void End(int tick)
        {
            if (!Watch.IsRunning)
            {
                return;
            }

            Watch.Stop();

            long elapsed = Watch.ElapsedTicks;
            totalRawTicks += elapsed;

            if (elapsed > maxRawTicks)
            {
                maxRawTicks = elapsed;
            }

            passes++;

            if (windowStartTick < 0)
            {
                windowStartTick = tick;
                return;
            }

            if (tick - windowStartTick < TicksPerDay)
            {
                return;
            }

            Report(tick - windowStartTick);
            windowStartTick = tick;
        }

        private static void Report(int ticksCovered)
        {
            double totalUs = RawToMicroseconds(totalRawTicks);
            double avgUs = passes > 0 ? totalUs / passes : 0.0;
            double maxUs = RawToMicroseconds(maxRawTicks);

            Log.Message(string.Format(
                CultureInfo.InvariantCulture,
                "[ClothingHygiene] perf: ticks={0} passes={1} total={2:0.#}us avg={3:0.###}us max={4:0.###}us",
                ticksCovered,
                passes,
                totalUs,
                avgUs,
                maxUs));

            totalRawTicks = 0L;
            maxRawTicks = 0L;
            passes = 0;
        }

        private static double RawToMicroseconds(long rawTicks)
        {
            return rawTicks * 1000000.0 / Stopwatch.Frequency;
        }
    }
}
