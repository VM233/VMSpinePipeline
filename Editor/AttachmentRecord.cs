using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    public sealed class AttachmentRecord
    {
        [VmRequired] public int slotIndex;
        [VmRequired] public string slotName;
        [VmRequired] public string name;
        [VmRequired] public string type;
        [VmRequired] public float x;
        [VmRequired] public float y;
        [VmRequired] public float width;
        [VmRequired] public float height;
    }
}
