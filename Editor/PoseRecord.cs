using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    public sealed class PoseRecord
    {
        [VmRequired] public float time;
        [VmRequired] public bool hasGeometry;
        [VmRequired] public BoundsRecord bounds;
        [VmRequired] public BonePoseRecord[] bones;
        [VmRequired] public SlotPoseRecord[] slots;
    }
}
