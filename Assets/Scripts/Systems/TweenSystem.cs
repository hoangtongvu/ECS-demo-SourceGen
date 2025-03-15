using Unity.Entities;

namespace TweenLib.Systems
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [Unity.Burst.BurstCompile]
    public partial struct TransformPositionTweener_TweenSystem : ISystem
    {
        [Unity.Burst.BurstCompile]
        public void OnCreate(ref Unity.Entities.SystemState state)
        {
            state.RequireForUpdate<Unity.Transforms.LocalTransform>();
            state.RequireForUpdate<Unity.Transforms.Can_TransformPositionTweener_TweenTag>();
            state.RequireForUpdate<Unity.Transforms.TransformPositionTweener_TweenData>();
        }

        [Unity.Burst.BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new TweenJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
            }.ScheduleParallel();

        }

        [Unity.Burst.BurstCompile]
        public partial struct TweenJob : IJobEntity
        {
            [Unity.Collections.ReadOnly] public float DeltaTime;

            [Unity.Burst.BurstCompile]
            void Execute(
                EnabledRefRW<Unity.Transforms.Can_TransformPositionTweener_TweenTag> canTweenTag
                , ref Unity.Transforms.LocalTransform component
                , ref Unity.Transforms.TransformPositionTweener_TweenData tweenData)
            {
                var tweener = new Utilities.TransformPositionTweener
                {
                    DeltaTime = this.DeltaTime,
                };

                if (tweener.CanStop(in component, in tweenData.LifeTimeSecond, in tweenData.BaseSpeed, in tweenData.Target))
                {
                    canTweenTag.ValueRW = false;
                    tweenData.LifeTimeSecond = 0f;
                    return;
                }

                tweener.Tween(ref component, in tweenData.BaseSpeed, in tweenData.Target);
                tweenData.LifeTimeSecond += this.DeltaTime;

            }

        }

    }

}
