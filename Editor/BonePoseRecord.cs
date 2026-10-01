using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    public sealed class BonePoseRecord
    {
        [VmRequired] public string name;
        [VmRequired] public float worldX;
        [VmRequired] public float worldY;
        [VmRequired] public float rotation;
    }
}
