# Security Checklist

## Per pull request

- [ ] No secrets, tokens, or real endpoints in code, tests, or docs
- [ ] New inputs validated (options → validators; request params → argument checks)
- [ ] No new logging of URLs-with-query, headers, or bodies
- [ ] Buffers/loops bounded (no unbounded reads, retries, or pagination)
- [ ] Exceptions not swallowed; failures normalized to `ApiError`
- [ ] New dependency? Full justification block + audit clean

## Per release

- [ ] `dotnet list package --vulnerable --include-transitive` clean
- [ ] CodeQL alerts triaged to zero open high/critical
- [ ] Dependabot PRs current
- [ ] Threat model reviewed if any new feature touches credentials, caching, or parsing
- [ ] SECURITY.md contact path verified working

## Standing posture (verified by tests)

- [x] Credential redaction in logs — `TelemetryTests`
- [x] Bounded error-body capture — `ErrorMappingTests`
- [x] Bodied requests never replayed on 401 — `AuthenticationTests`
- [x] Cache restricted to GET+200 with size bounds — `CachingTests`
- [x] Pagination bounded — `PaginationTests`
- [x] Timeout vs caller-cancellation distinguished — `ErrorMappingTests`
