# Definition of Done

A task/feature is done when ALL of the following hold:

1. **Builds** on `net8.0`, `net9.0`, `net10.0` with zero warnings (`TreatWarningsAsErrors`).
2. **Tested**: happy path, invalid input, failure path, cancellation (where async), and thread-safety (where shared state) — executed and green, not just written.
3. **Documented**: XML docs on all public members; feature guide updated; CHANGELOG `Unreleased` entry.
4. **Observable**: logs/traces/metrics emitted where the feature does I/O; redaction verified.
5. **Secure**: no secrets in logs/URLs/errors; inputs validated; resources disposed; no unbounded memory/loops.
6. **API-reviewed**: checklist in docs/api/public-api-review-checklist.md applied.
7. **Packaged**: `dotnet pack` still succeeds with package validation.
8. **No placeholders**: no `NotImplementedException`, no `TODO` in shipped code paths.
