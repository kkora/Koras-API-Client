# Upgrading

## General procedure

1. Read the CHANGELOG sections between your version and the target (especially **Breaking** and **Deprecated**).
2. Bump all `Koras.ApiClient*` packages together — they version in lockstep.
3. Rebuild with warnings visible: obsoletion messages contain their own migration hints.
4. Run your test suite — if you test through `MockApiHandler` (recommended), behavioral changes surface immediately.

## Version-specific guides

| From → To | Guide |
|---|---|
| (first release) | nothing to migrate yet — guides land here per release |

## Staying informed

Watch GitHub Releases; every release's notes link the CHANGELOG entry and any migration doc.
