namespace ClothingHygiene.Domain
{
    /// <summary>폰이 지금 무엇을 하고 있는가. 계수 결정에만 쓰인다.</summary>
    public enum ActivityKind
    {
        Sleep = 0,
        Idle = 1,
        Labor = 2,
        Strenuous = 3
    }

    /// <summary>오염 누적 속도 계산. 순수 함수만 둔다.</summary>
    public static class FilthRates
    {
        public const int TicksPerDay = 60000;

        public const float HotThreshold = 30f;
        public const float HotFactor = 1.3f;
        public const float ColdThreshold = 0f;
        public const float ColdFactor = 0.7f;

        private static readonly float[] ActivityFactor = { 0.4f, 1f, 1.5f, 2.5f };

        public static float Activity(ActivityKind kind)
        {
            return ActivityFactor[(int)kind];
        }

        public static float Temperature(float celsius)
        {
            if (celsius >= HotThreshold)
            {
                return HotFactor;
            }

            if (celsius <= ColdThreshold)
            {
                return ColdFactor;
            }

            return 1f;
        }

        /// <summary>한 간격(deltaTicks) 동안 늘어날 오염도.</summary>
        public static float Delta(float filthPerDayPercent, int deltaTicks, float activityFactor, float temperatureFactor, float garmentFactor)
        {
            return filthPerDayPercent / 100f / TicksPerDay * deltaTicks * activityFactor * temperatureFactor * garmentFactor;
        }
    }
}
