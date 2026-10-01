using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    public sealed class AnimationRecord
    {
        [VmRequired] public string name;
        [VmRequired] public float duration;
        [VmRequired] public int timelineCount;
    }
}
