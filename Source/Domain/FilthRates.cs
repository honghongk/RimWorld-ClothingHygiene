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

        /// <summary>바닐라 오물(발에 묻은 Filth)을 지닌 채 돌아다니면 오염이 빨라진다.</summary>
        public const float CarriedFilthFactor = 1.5f;

        // 재질 계열별 오염 배율. 금속 갑옷은 거의 안 더러워지고, 천은 빨리 더러워진다.
        private static readonly float[] MaterialFactor = { 1f, 1.3f, 1.1f, 0.6f, 0.5f, 0.25f };

        public static float Material(GarmentMaterial material)
        {
            return MaterialFactor[(int)material];
        }

        public static float CarriedFilth(bool carryingFilth)
        {
            return carryingFilth ? CarriedFilthFactor : 1f;
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
