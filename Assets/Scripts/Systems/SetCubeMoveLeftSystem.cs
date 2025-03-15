using Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct SetCubeMoveLeftSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LocalTransform>();
            state.RequireForUpdate<Can_TransformPositionTweener_TweenTag>();
            state.RequireForUpdate<TransformPositionTweener_TweenData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (transformRef, moveStateRef, tweenDataRef, canTweenTag, moveStateChangedTag) in
                SystemAPI.Query<
                    RefRO<LocalTransform>
                    , RefRO<MoveStateICD>
                    , RefRW<TransformPositionTweener_TweenData>
                    , EnabledRefRW<Can_TransformPositionTweener_TweenTag>
                    , EnabledRefRO<MoveStateChangedTag>>()
                    .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                if (!moveStateChangedTag.ValueRO) continue;
                if (moveStateRef.ValueRO.Value != MoveState.Left) continue;

                const float leftX = -8f;
                const float baseSpeed = 2f;

                tweenDataRef.ValueRW.BaseSpeed = baseSpeed;
                tweenDataRef.ValueRW.Target = new float3(leftX, 0, 0);

                canTweenTag.ValueRW = true;

            }

        }

    }

}
