using ClothingHygiene.Domain;

namespace ClothingHygiene.Application
{
    /// <summary>의복 한 벌의 오염 누적 유스케이스. 게임 타입을 모른다(도메인 규칙 조합만).</summary>
    public static class HygieneTickUseCase
    {
        public static void AccumulateOne(IHygieneState state, float filthPerDayPercent, int deltaTicks, ActivityKind activity, float temperatureCelsius)
        {
            if (state == null || !state.IsWorn)
            {
                return;
            }

            float delta = FilthRates.Delta(
                filthPerDayPercent,
                deltaTicks,
                FilthRates.Activity(activity),
                FilthRates.Temperature(temperatureCelsius),
                state.GarmentFactor);

            state.Filth = HygieneValue.Add(state.Filth, delta);
        }
    }
}
