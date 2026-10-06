# Static Cost Ledger

## Weighted Mesh inspection addition (2026-10-06)

Frozen replacement: 19 bones, 15 slots, one skin, 18 attachments, 12 animations;
Weapon/8-1 is a weighted mesh with 16 vertices and 19 triangles. Metadata reads
add only scalar array lengths, nullable parent identity and the existing native
IHasTextureRegion reference. No vertex traversal, copying, hierarchy scan or
cache is added. The existing maxRecords and response size owners still bound
record materialization. Region transforms remain region-only; mesh dimensions
are Spine metadata in imported skeleton units, not transformed bounds. The
independent pose sampler remains the authority for geometry bounds.

Two focused tests use one three-vertex weighted mesh, one linked clone, one slot,
one temporary material and one 2x2 texture. Native material and texture lifecycles
are paired. Tests perform one metadata read and one slot read each; retained
arrays and records remain below 8 KiB. PASS.

Frozen migration inventory: one Warrior dataset, old/new bones 19/16, slots 10/15,
skins 3/1, skin entries 17/18 and animations 8/8. New animation keyframes total
207; the largest individual animation contains 34 keyframes. The source export
contains three files. No tool scans the AssetDatabase or asset directories.

Inspection loads one skeleton asset and its assigned controller. Metadata records
are counted before result materialization; skins contribute their attachment
counts without a skins-by-slots cross-product. `maxRecords` is 1..8192, default
2048. Work and result memory are linear in the accepted record count. At most one
controller subasset enumeration and explicit atlas/material reads occur. Atlas,
material and controller clip records also count toward the report budget.

Sampling accepts 1..16 times. Before constructing samples it checks the combined
bone, slot, skin, attachment, animation and selected-timeline record count and
the sample output product against `maxRecords`. Summed weighted vertex storage
and selected animation frame storage are each at most 65536 elements. Worst
case is 16 independent poses, at most 1048576 vertex-element visits and the
bounded selected-timeline applications. There is one skeleton, one reusable
vertex buffer and at most 8192 returned records; source data remains shared and
immutable. All calls execute synchronously on the Editor main thread, with no
worker Unity access, persistent cache or per-frame loop. CLI budget: 30 seconds.

Frozen Warrior sampling uses five times per animation: at most 8 * 5 * 15 = 600
slot observations and 8 * 5 * 16 = 640 bone observations, in eight sequential
requests. Every attachment is a region, so one pose visits at most 15 * 8 = 120
vertex elements. Across all requests that is at most 4800 vertex elements.

The focused tests construct one bone, one slot, one skin, two region attachments
and one attachment timeline. They perform at most three poses and no Unity asset
scan. Status: PASS for the frozen inventory and declared accepted domains.

Live Mecanim sampling resolves exactly one object through Automation's object ID
owner. Its accepted domain is paused Play Mode, one active SkeletonMecanim and one
flat AnimatorController layer with direct AnimationClip motions. It admits only
metadata, state, parameter, timeline and output records within maxRecords, and the
same 65536 geometry/frame-element budgets. No hierarchy or asset scan occurs.
The native Animator and Spine owners each update once with delta time zero; one
mesh update follows. No frame waiting, retries or texture cloning is introduced.
Frozen Warrior: one layer, six states, fewer than ten parameters, 16 bones, 15 slots,
eight animations, 18 source entries, ten equipment variants and 207 source keyframes.
Each call returns fifteen slot records and one world XY bound. External gameplay
callbacks retain their own budgets; the current equipment callback visits at most
seven hidden slots. Status: PASS for the frozen inventory and admitted domains.
