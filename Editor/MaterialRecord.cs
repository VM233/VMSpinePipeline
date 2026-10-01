using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    public sealed class MaterialRecord
    {
        [VmRequired] public string assetPath;
        [VmRequired] public string texturePath;
        [VmRequired] public string shaderName;
    }
}
