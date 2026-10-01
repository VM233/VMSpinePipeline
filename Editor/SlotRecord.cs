using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    public sealed class SlotRecord
    {
        [VmRequired] public int index;
        [VmRequired] public string name;
        [VmRequired] public string boneName;
        [VmRequired] public string setupAttachmentName;
    }
}
