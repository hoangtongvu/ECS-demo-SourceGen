using Core.Utilities.Extensions;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

namespace TweenLib.Systems
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [Unity.Burst.BurstCompile]
    public partial struct TransformPositionTweener_TweenSystem : ISystem
    {
        private EntityQuery query;
        private ComponentTypeHandle<Unity.Transforms.LocalTransform> componentTypeHandle;
        private ComponentTypeHandle<Unity.Transforms.Can_TransformPositionTweener_TweenTag> canTweenTagTypeHandle;
        private ComponentTypeHandle<Unity.Transforms.TransformPositionTweener_TweenData> tweenDataTypeHandle;

        [Unity.Burst.BurstCompile]
        public void OnCreate(ref Unity.Entities.SystemState state)
        {
            EntityQueryBuilder queryBuilder = new EntityQueryBuilder(Allocator.Temp);

            this.query = queryBuilder
                .WithAllRW<Unity.Transforms.LocalTransform>()
                .WithAllRW<Unity.Transforms.TransformPositionTweener_TweenData>()
                .WithAll<Unity.Transforms.Can_TransformPositionTweener_TweenTag>()
                .Build(ref state);

            queryBuilder.Dispose();

            this.componentTypeHandle = state.GetComponentTypeHandle<Unity.Transforms.LocalTransform>(false);
            this.canTweenTagTypeHandle = state.GetComponentTypeHandle<Unity.Transforms.Can_TransformPositionTweener_TweenTag>(false);
            this.tweenDataTypeHandle = state.GetComponentTypeHandle<Unity.Transforms.TransformPositionTweener_TweenData>(false);

            state.RequireForUpdate<Unity.Transforms.LocalTransform>();
            state.RequireForUpdate<Unity.Transforms.Can_TransformPositionTweener_TweenTag>();
            state.RequireForUpdate<Unity.Transforms.TransformPositionTweener_TweenData>();
        }

        [Unity.Burst.BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            this.componentTypeHandle.Update(ref state);
            this.canTweenTagTypeHandle.Update(ref state);
            this.tweenDataTypeHandle.Update(ref state);

            state.Dependency = new TweenIJC
            {
                DeltaTime = state.WorldUnmanaged.Time.DeltaTime,
                ComponentTypeHandle = this.componentTypeHandle,
                CanTweenTagTypeHandle = this.canTweenTagTypeHandle,
                TweenDataTypeHandle = this.tweenDataTypeHandle,
            }.ScheduleParallel(this.query, state.Dependency);

        }

        [Unity.Burst.BurstCompile]
        public struct TweenIJC : IJobChunk
        {
            [Unity.Collections.ReadOnly] public float DeltaTime;
            public ComponentTypeHandle<Unity.Transforms.LocalTransform> ComponentTypeHandle;
            public ComponentTypeHandle<Unity.Transforms.Can_TransformPositionTweener_TweenTag> CanTweenTagTypeHandle;
            public ComponentTypeHandle<Unity.Transforms.TransformPositionTweener_TweenData> TweenDataTypeHandle;

            [Unity.Burst.BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var canTweenTagEnabledMask_RW = chunk.GetEnabledMask(ref this.CanTweenTagTypeHandle);
                var componentArray = chunk.GetNativeArray(ref this.ComponentTypeHandle);
                var tweenDataArray = chunk.GetNativeArray(ref this.TweenDataTypeHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);

                while (enumerator.NextEntityIndex(out var i))
                {
                    ref var component = ref componentArray.ElementAt(i);
                    ref var tweenData = ref tweenDataArray.ElementAt(i);
                    var canTweenTag = canTweenTagEnabledMask_RW.GetEnabledRefRW<Unity.Transforms.Can_TransformPositionTweener_TweenTag>(i);

                    var tweener = new Utilities.TransformPositionTweener
                    {
                        DeltaTime = this.DeltaTime,
                    };

                    if (tweener.CanStop(in component, in tweenData.LifeTimeSecond, in tweenData.BaseSpeed, in tweenData.Target))
                    {
                        canTweenTag.ValueRW = false;
                        tweenData.LifeTimeSecond = 0f;
                        continue;
                    }

                    tweener.Tween(ref component, in tweenData.BaseSpeed, in tweenData.Target);
                    tweenData.LifeTimeSecond += this.DeltaTime;

                }

            }

        }

    }

}
