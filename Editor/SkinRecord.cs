using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    public sealed class SkinRecord
    {
        [VmRequired] public string name;
        [VmRequired] public AttachmentRecord[] attachments;
    }
}
