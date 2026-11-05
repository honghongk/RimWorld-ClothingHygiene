namespace ClothingHygiene.Domain
{
    /// <summary>
    /// 오염 상태를 가진 것(의복 한 벌). Domain 이 게임 타입을 모르게 하는 유일한 접점.
    /// </summary>
    public interface IHygieneState
    {
        float Filth { get; set; }

        bool IsWorn { get; }

        /// <summary>의복 종류별 오염 배율(얇은 옷 &lt; 1 &lt; 방한복).</summary>
        float GarmentFactor { get; }
    }
}
