using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    public sealed class MeshSpriteCoverageResult
    {
        [VmRequired] public bool success;
        [VmRequired] public string spriteAssetPath;
        [VmRequired] public string spriteGuid;
        [VmRequired] public int opaquePixels;
        [VmRequired] public int uncoveredOriginalPixels;
        [VmRequired] public int uncoveredExpandedPixels;
        [VmRequired] public int originalVertexOffset;
        [VmRequired] public int originalVertexCount;
        [VmRequired] public int expandedVertexCount;
        [VmRequired] public int originalTriangleCount;
        [VmRequired] public int expandedTriangleCount;
        [VmRequired] public float[] expandedRegionUVs;
        [VmRequired] public int[] expandedTriangles;
        [VmRequired] public MeshPosePreservationRecord[] poses;
    }
}
