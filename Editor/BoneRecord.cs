using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    public sealed class BoneRecord
    {
        [VmRequired] public string name;
        [VmRequired] public string parent;
        [VmRequired] public float x;
        [VmRequired] public float y;
        [VmRequired] public float rotation;
        [VmRequired] public float scaleX;
        [VmRequired] public float scaleY;
    }
}
