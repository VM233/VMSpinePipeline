using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    public sealed class SlotPoseRecord
    {
        [VmRequired] public string name;
        [VmRequired] public string boneName;
        [VmRequired] public string attachmentName;
        [VmRequired] public bool hasAttachment;
    }
}
