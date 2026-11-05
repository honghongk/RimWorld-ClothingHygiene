using Verse;

namespace ClothingHygiene.Infrastructure
{
    public class CompProperties_ClothingHygiene : CompProperties
    {
        /// <summary>이 의복의 오염 배율. 얇은 옷 &lt; 1 &lt; 방한복.</summary>
        public float filthFactor = 1f;

        public CompProperties_ClothingHygiene()
        {
            compClass = typeof(ThingComp_ClothingHygiene);
        }
    }
}
