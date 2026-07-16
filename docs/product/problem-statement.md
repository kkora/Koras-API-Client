# Problem Statement

## The problem

Teams repeatedly rebuild HTTP client infrastructure for authentication, retries, timeout handling, pagination, logging, caching, and error normalization. In a typical enterprise codebase this appears as:

- 5–15 hand-written `DelegatingHandler` implementations per organization, subtly different
- Inconsistent retry policies: some unbounded, some retrying non-idempotent requests, some ignoring `Retry-After`
- Pagination loops duplicated per external API, each with its own bugs (off-by-one pages, lost cancellation, unbounded loops)
- Error handling split between exceptions, nulls, and status-code checks — impossible to handle uniformly in calling code
- Missing or inconsistent correlation IDs, making cross-service debugging expensive
- Secrets accidentally logged in request URLs or headers

## Consequences of the status quo

| Consequence | Cost |
|---|---|
| Re-implementation per service | Days of engineering per integration |
| Policy drift | Incidents caused by one service retrying aggressively during an outage |
| Inconsistent errors | Defensive code everywhere; poor API consumer experience |
| Missing observability | Longer MTTR; no per-dependency dashboards |
| Security gaps | Credentials in logs and URLs; unvalidated TLS shortcuts copy-pasted |

## Who feels the pain

- **API developers** integrating third-party REST APIs
- **Microservice teams** making service-to-service calls
- **SDK authors** who need a dependable foundation beneath a vendor SDK
- **Platform/integration teams** trying to enforce organization-wide HTTP standards

## Why now

`Microsoft.Extensions.Http.Resilience` (Polly v8-based) finally standardized the resilience primitives, and .NET 8+ standardized `TimeProvider`, keyed DI, and metrics. The missing piece is a cohesive client framework that composes these into a product teams can adopt in one line of DI registration. That is Koras.ApiClient.
