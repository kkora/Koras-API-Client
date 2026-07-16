# Public API Review Checklist

Apply to every PR that touches `public` or `protected` surface:

- [ ] Is the member necessary now, or can it stay `internal` until a consumer needs it?
- [ ] XML docs complete (summary, params, returns, exceptions, thread-safety note where relevant)?
- [ ] Async? Then: `Async` suffix, `CancellationToken` last with default, `Task`/`ValueTask` justified, `ConfigureAwait(false)` inside.
- [ ] Nullability annotations honest (no `!` hiding a real null path)?
- [ ] No third-party type in the signature (except approved seams: `IHttpClientBuilder`, `HttpStandardResilienceOptions`, `IConfiguration`)?
- [ ] No boolean-flag overload pairs; enums or options instead?
- [ ] Options: mutable POCO, validated in `KorasApiClientOptionsValidator` or a feature validator, safe defaults?
- [ ] Exceptions documented and limited to argument/state/cancellation; runtime failures return `ApiError`?
- [ ] Thread-safety statement matches registration lifetime (singleton ⇒ thread-safe)?
- [ ] Naming per docs/api/naming-guidelines.md?
- [ ] Tests cover the new surface (happy, invalid input, cancellation where applicable)?
- [ ] Docs + samples updated; CHANGELOG `Unreleased` entry added?
- [ ] For changes to existing surface: is it breaking? If yes, follows docs/api/backward-compatibility.md process?
