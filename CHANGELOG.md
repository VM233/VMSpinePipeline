# Changelog

## 0.1.0

- Add typed skeleton-data inspection, including bones, slots, skins, attachment
  geometry, material references, animation durations and Mecanim clip identity.
- Add deterministic Spine animation pose sampling without entering Play Mode,
  changing source data or creating Unity GameObjects.
- Reject incompatible exports, missing authoring references and requests exceeding
  explicit output or geometry budgets with stable errors.
