namespace ClothingHygiene.Domain
{
    /// <summary>
    /// 세탁에 쓰는 물. DBH 배관이든 자체 물통이든 이 계약으로만 다룬다(Domain 은 구현을 모른다).
    /// </summary>
    public interface IWaterSource
    {
        /// <summary>지금 물을 쓸 수 있는가.</summary>
        bool Available { get; }

        /// <summary>UI 에 표시할 번역 키. 예: CH_Water_Dubwise</summary>
        string LabelKey { get; }

        /// <summary>물을 소비한다. 부족하면 false.</summary>
        bool TryConsume(float amount);
    }
}
