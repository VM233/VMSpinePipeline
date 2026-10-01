using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    public sealed class AtlasRecord
    {
        [VmRequired] public string assetPath;
        [VmRequired] public MaterialRecord[] materials;
    }
}
