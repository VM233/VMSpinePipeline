using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    public sealed class MeshPosePreservationRecord
    {
        [VmRequired] public float time;
        [VmRequired] public double maxOriginalVertexError;
        [VmRequired] public bool allVerticesFinite;
    }
}
