using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    public sealed class ClipRecord
    {
        [VmRequired] public string name;
        [VmRequired] public string localFileId;
        [VmRequired] public float duration;
        [VmRequired] public bool hasSpineAnimation;
    }
}
