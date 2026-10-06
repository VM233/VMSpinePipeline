# VM Spine Pipeline

Editor tools for inspecting Spine skeleton data and sampling animation poses,
including paused live SkeletonMecanim states,
through the official Unity CLI, VM Unity Pipeline and VM Unity Automation.
Package ID: `com.vm233.spine-pipeline`.

Requirements: Unity 6000.4, an independently licensed Spine-Unity 4.2 installation
with the `spine-csharp` and `spine-unity` assemblies, VM Unity Automation 0.6.69
or later, and the official Unity Pipeline transport. Spine is installed separately;
this package does not distribute the Spine runtime or game artwork.

Install from an immutable Git revision:
`https://github.com/VM233/VMSpinePipeline.git#<full-commit-sha>`.
Pin the same Automation dependency used by your project before package resolution.

`Editor/` owns the tools and typed contracts. `Tests/Editor/` contains focused
EditMode tests. Discover this package through `vm_catalog_list` using the package
ID, then request one exact contract with `vm_catalog_get` before invoking it.

The skeleton-data and mesh coverage readers are read-only. The live Mecanim sampler changes
only the paused runtime pose, with an explicit component ID and state name.

`Runtime/SpineMeshRectangle.Expand` prepares bone-driven meshes for rectangular
equipment Sprites. It preserves authored vertex records and triangles, adding
four outer vertices and exterior triangles. Pass an independent setup-pose Slot;
cache the result with the skeleton binding and remap Sprites through Spine's
native attachment tools. Vertex-deform timelines and sequences are explicitly
unsupported. The source skeleton and its assets are not modified.

`spine/inspect-mesh-sprite-coverage` compares native Sprite alpha coverage before
and after expansion and samples the original vertices under explicit animation
times. It reports the expanded UV topology and exact original-vertex errors.
See the [geometry contract and cost ledger](Documentation~/MeshRectangleCostLedger.md).

See [Integration](Documentation~/Integration.md),
[Cost ledger](Documentation~/StaticCostLedger.md), [CHANGELOG](CHANGELOG.md),
[LICENSE](LICENSE), and [third-party requirements](THIRD-PARTY-NOTICES.md).
