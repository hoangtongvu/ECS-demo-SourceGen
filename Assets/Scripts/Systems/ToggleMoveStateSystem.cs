using Components;
using Unity.Entities;
using UnityEngine;

namespace Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial class ToggleMoveStateSystem : SystemBase
    {
        protected override void OnCreate()
        {
            this.RequireForUpdate<MoveStateChangedTag>();
            this.RequireForUpdate<MoveStateICD>();
        }

        protected override void OnUpdate()
        {
            // Reset tags
            foreach (var moveStateChangedTag in
                SystemAPI.Query<
                    EnabledRefRW<MoveStateChangedTag>>())
            {
                moveStateChangedTag.ValueRW = false;
            }


            if (!Input.GetKeyDown(KeyCode.F1)) return;

            foreach (var (moveStateRef, moveStateChangedTag) in
                SystemAPI.Query<
                    RefRW<MoveStateICD>
                    , EnabledRefRW<MoveStateChangedTag>>()
                    .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                moveStateChangedTag.ValueRW = true;

                if (moveStateRef.ValueRO.Value == MoveState.Left)
                {
                    moveStateRef.ValueRW.Value = MoveState.Right;
                    continue;
                }

                moveStateRef.ValueRW.Value = MoveState.Left;

            }

        }

    }

}
