using TweenLib;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;

namespace Utilities
{
    [BurstCompile]
    public partial struct TransformPositionTweener : ITweener<LocalTransform, float3>
    {
        [BurstCompile]
        public bool CanStop(in LocalTransform componentData, in float lifeTimeSecond, in float baseSpeed, in float3 target)
        {
            const float epsilon = 0.01f;
            return math.all(math.abs(target - componentData.Position) < new float3(epsilon));
        }

        [BurstCompile]
        public void Tween(ref LocalTransform componentData, in float baseSpeed, in float3 target)
        {
            componentData.Position =
                math.lerp(componentData.Position, target, baseSpeed * this.DeltaTime);
        }

    }

}
