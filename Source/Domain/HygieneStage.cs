namespace ClothingHygiene.Domain
{
    public enum HygieneStage
    {
        Clean = 0,
        Worn = 1,
        Smelly = 2,
        Clammy = 3,
        Filthy = 4
    }

    /// <summary>단계 경계·무드 수치·번역 키. 배열 인덱스 = enum 값.</summary>
    public static class HygieneStageInfo
    {
        public const int Count = 5;

        private static readonly float[] UpperBound = { 0.2f, 0.5f, 0.7f, 0.9f, 1.01f };

        private static readonly int[] MoodOffset = { 1, 0, -3, -6, -10 };

        private static readonly string[] LabelKey =
        {
            "CH_Stage_Clean",
            "CH_Stage_Worn",
            "CH_Stage_Smelly",
            "CH_Stage_Clammy",
            "CH_Stage_Filthy"
        };

        public static HygieneStage Of(float filth)
        {
            for (int i = 0; i < UpperBound.Length; i++)
            {
                if (filth < UpperBound[i])
                {
                    return (HygieneStage)i;
                }
            }

            return HygieneStage.Filthy;
        }

        public static float UpperBoundOf(HygieneStage stage)
        {
            return UpperBound[(int)stage];
        }

        public static int MoodOf(HygieneStage stage)
        {
            return MoodOffset[(int)stage];
        }

        public static bool IsDebuff(HygieneStage stage)
        {
            return MoodOffset[(int)stage] < 0;
        }

        public static bool IsClean(HygieneStage stage)
        {
            return stage == HygieneStage.Clean;
        }

        public static string LabelKeyOf(HygieneStage stage)
        {
            return LabelKey[(int)stage];
        }
    }
}
