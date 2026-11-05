namespace ClothingHygiene.Domain
{
    /// <summary>오염도 값 연산. 0.0 = 깨끗함, 1.0 = 완전히 더러움. 게임 타입에 의존하지 않는다.</summary>
    public static class HygieneValue
    {
        public const float Min = 0f;
        public const float Max = 1f;

        /// <summary>이 값 미만이면 깨끗한 옷으로 취급한다.</summary>
        public const float CleanThreshold = 0.02f;

        public static float Clamp(float value)
        {
            if (value < Min)
            {
                return Min;
            }

            if (value > Max)
            {
                return Max;
            }

            return value;
        }

        public static float Add(float current, float delta)
        {
            return Clamp(current + delta);
        }
    }
}
