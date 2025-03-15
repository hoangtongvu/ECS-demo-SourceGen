using Components;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Authoring.Camera
{
    public class CubeAuthoring : MonoBehaviour
    {
        private class Baker : Baker<CubeAuthoring>
        {
            public override void Bake(CubeAuthoring authoring)
            {
                Entity entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

                AddComponent<Can_TransformPositionTweener_TweenTag>(entity);
                SetComponentEnabled<Can_TransformPositionTweener_TweenTag>(entity, false);

                AddComponent<TransformPositionTweener_TweenData>(entity);

                AddComponent(entity, new MoveStateICD
                {
                    Value = MoveState.Left,
                });

                AddComponent<MoveStateChangedTag>(entity);
                SetComponentEnabled<MoveStateChangedTag>(entity, false);

            }
        }

    }

}
