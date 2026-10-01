using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    public sealed class MecanimPoseResult
    {
        [VmRequired] public string instanceId;
        [VmRequired] public string stateName;
        [VmRequired] public string animationName;
        [VmRequired] public float normalizedTime;
        [VmRequired] public float animationDuration;
        [VmRequired] public int recordCount;
        [VmRequired] public int meshVertexCount;
        [VmRequired] public BoundsRecord worldBounds;
        [VmRequired] public MecanimSlotRecord[] slots;
    }
}
