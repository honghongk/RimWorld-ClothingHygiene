using ClothingHygiene.Domain;
using UnityEngine;
using Verse;

namespace ClothingHygiene.Infrastructure
{
    public class HygieneMod : Mod
    {
        public static HygieneSettings Settings;

        public HygieneMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<HygieneSettings>();
        }

        public override string SettingsCategory()
        {
            return "Clothing Hygiene";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("CH_Settings_FilthPerDay".Translate(Settings.filthPerDay.ToString("0")));
            Settings.filthPerDay = listing.Slider(Settings.filthPerDay, 10f, 200f);

            listing.Gap(6f);
            listing.Label("CH_Settings_ChangeThreshold".Translate(Settings.changeThresholdPercent.ToString("0")));
            Settings.changeThresholdPercent = listing.Slider(Settings.changeThresholdPercent, 30f, 100f);

            listing.Gap(6f);
            listing.Label("CH_Settings_DurabilityLoss".Translate(Settings.laundryDurabilityLossPercent.ToString("0.0")));
            Settings.laundryDurabilityLossPercent = listing.Slider(Settings.laundryDurabilityLossPercent, 0f, 10f);

            listing.Gap(6f);
            listing.Label("CH_Settings_MoodScale".Translate(Settings.moodPenaltyScale.ToString("0.0")));
            Settings.moodPenaltyScale = listing.Slider(Settings.moodPenaltyScale, 0f, 2f);

            listing.Gap(6f);
            listing.CheckboxLabeled("CH_Settings_MoodEnabled".Translate(), ref Settings.moodEnabled, "CH_Settings_MoodEnabledTip".Translate());

            listing.Gap(6f);
            listing.Label("CH_Settings_WaterMode".Translate());
            if (listing.ButtonText(("CH_WaterMode_" + Settings.waterMode).Translate()))
            {
                Settings.waterMode = Next(Settings.waterMode);
            }

            listing.End();
        }

        private static WaterMode Next(WaterMode mode)
        {
            int next = (int)mode + 1;

            if (next > (int)WaterMode.RequireWater)
            {
                next = 0;
            }

            return (WaterMode)next;
        }
    }

    public class HygieneSettings : ModSettings
    {
        public float filthPerDay = 65f;
        public float changeThresholdPercent = 70f;
        public float laundryDurabilityLossPercent = 2f;
        public float moodPenaltyScale = 1f;
        public int cleanBonusCap = 5;
        public float soaplessCleanFloor = 0.4f;
        public bool moodEnabled = true;
        public WaterMode waterMode = WaterMode.Auto;

        public float ChangeThreshold
        {
            get { return changeThresholdPercent / 100f; }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref filthPerDay, "ch_filthPerDay", 65f);
            Scribe_Values.Look(ref changeThresholdPercent, "ch_changeThreshold", 70f);
            Scribe_Values.Look(ref laundryDurabilityLossPercent, "ch_durabilityLoss", 2f);
            Scribe_Values.Look(ref moodPenaltyScale, "ch_moodScale", 1f);
            Scribe_Values.Look(ref cleanBonusCap, "ch_cleanBonusCap", 5);
            Scribe_Values.Look(ref soaplessCleanFloor, "ch_soaplessFloor", 0.4f);
            Scribe_Values.Look(ref moodEnabled, "ch_moodEnabled", true);
            Scribe_Values.Look(ref waterMode, "ch_waterMode", WaterMode.Auto);
        }
    }
}
