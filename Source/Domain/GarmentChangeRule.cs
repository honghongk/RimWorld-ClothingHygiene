namespace ClothingHygiene.Domain
{
    /// <summary>물을 어디서 얻는가. DBH 유무에 따라 Infra 가 해석한다.</summary>
    public enum WaterMode
    {
        /// <summary>DBH 가 있으면 배관 물, 없으면 물 없이.</summary>
        Auto = 0,

        /// <summary>항상 물 없이 세탁.</summary>
        NoWater = 1,

        /// <summary>물이 없으면 세탁 불가(DBH 배관 또는 물통 필요).</summary>
        RequireWater = 2
    }

    /// <summary>갈아입기 판단. 순수 조건식만.</summary>
    public static class GarmentChangeRule
    {
        public static bool ShouldChange(float filth, float threshold, bool cleanSpareAvailable)
        {
            return filth >= threshold && cleanSpareAvailable;
        }

        public static bool IsCleanSpare(float filth, float hitPointRatio, float maxFilth, float minHitPointRatio)
        {
            return filth < maxFilth && hitPointRatio > minHitPointRatio;
        }
    }
}
