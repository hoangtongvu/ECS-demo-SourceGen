using Unity.Entities;

namespace Components
{
    public enum MoveState
    {
        None = 0,
        Left = 1,
        Right = 2,
    }

    public struct MoveStateICD : IComponentData
    {
        public MoveState Value;
    }

}
