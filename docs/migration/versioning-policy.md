# Versioning Policy (migration view)

The authoritative policy lives in [release/versioning.md](../release/versioning.md). For consumers:

- **Patch** upgrades are always safe — take them automatically (Dependabot-friendly).
- **Minor** upgrades are additive; while the package is pre-1.0, read the CHANGELOG "Breaking" section first (SemVer §4 allows breaks in 0.x minors — we flag every one).
- **Major** upgrades come with a migration guide in this folder and obsoletion warnings at least one minor in advance.
- Compile-time surface changes are guarded by package validation and (post-1.0) PublicAPI baselines, so unintentional breaks should never reach you.
