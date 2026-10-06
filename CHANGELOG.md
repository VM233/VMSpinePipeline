# Changelog

## 0.3.1

- Use a texture-capable native shader and valid atlas dimensions in the linked
  Mesh fixture, and verify its actual texture binding before inspecting the slot.
  Unity's internal error shader does not provide the required texture property.

## 0.3.0

- Report weighted and linked Mesh attachment topology and dimensions in skeleton
  inspection and live Mecanim samples. Preserve Region rotation and scale metadata.
- Resolve live Mesh material and texture identities through Spine's native
  IHasTextureRegion owner; Mesh slots no longer report empty resource identities.
- Cover weighted source metadata and linked Mesh resource sharing in focused tests.

## 0.2.0

- Sample live, paused SkeletonMecanim states through the actual Animator, Spine
  callbacks and mesh owners, with rendered bounds and material/texture identities.
- Limit live sampling to one flat AnimatorController layer with direct clips.

## 0.1.2

- Keep each typed request and result in its own source file for code policy review.
- Cover inherited bone world rotation in the focused Editor tests.

## 0.1.1

- Report sampled bone rotation in skeleton-local world space, alongside world X/Y.
- Normalize new package metadata without changing GUIDs.

## 0.1.0

- Add typed skeleton-data inspection, including bones, slots, skins, attachment
  geometry, material references, animation durations and Mecanim clip identity.
- Add deterministic Spine animation pose sampling without entering Play Mode,
  changing source data or creating Unity GameObjects.
- Reject incompatible exports, missing authoring references and requests exceeding
  explicit output or geometry budgets with stable errors.
