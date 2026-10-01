using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    public sealed class SkeletonPoseResult
    {
        [VmRequired] public string assetPath;
        [VmRequired] public string skinName;
        [VmRequired] public string animationName;
        [VmRequired] public float animationDuration;
        [VmRequired] public int recordCount;
        [VmRequired] public PoseRecord[] poses;
    }
}
