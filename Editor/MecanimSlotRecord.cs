using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    public sealed class MecanimSlotRecord
    {
        [VmRequired] public string name;
        [VmRequired] public string boneName;
        [VmRequired] public bool hasAttachment;
        [VmRequired] public string attachmentName;
        [VmRequired] public string attachmentType;
        [VmRequired] public float x;
        [VmRequired] public float y;
        [VmRequired] public float rotation;
        [VmRequired] public float scaleX;
        [VmRequired] public float scaleY;
        [VmRequired] public float width;
        [VmRequired] public float height;
        [VmRequired] public string textureName;
        [VmRequired] public string textureInstanceId;
        [VmRequired] public string materialInstanceId;
    }
}
