# VM Spine Pipeline

Editor tools for inspecting Spine skeleton data and sampling animation poses
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

See [Integration](Documentation~/Integration.md),
[Cost ledger](Documentation~/StaticCostLedger.md), [CHANGELOG](CHANGELOG.md),
[LICENSE](LICENSE), and [third-party requirements](THIRD-PARTY-NOTICES.md).
