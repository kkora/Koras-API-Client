# Test Matrix

| Feature | Happy | Invalid input | Failure path | Cancellation | Concurrency | Integration |
|---|---|---|---|---|---|---|
| Request builder | ✅ query/header/body tests | ✅ empty names, bad timeout | ✅ via error mapping | ✅ pre-cancelled token | n/a (documented non-thread-safe) | ✅ echo server |
| Error model | ✅ success paths | ✅ malformed problem JSON | ✅ 13-row status table, network, timeout | ✅ cancellation vs timeout | n/a | ✅ real ASP.NET ProblemDetails |
| API key auth | ✅ header/query | ✅ empty key (ctor + startup) | ✅ n/a | inherited | stateless | — |
| Bearer auth | ✅ attach | — | ✅ 401 no-retry-with-body | inherited | provider contract | — |
| Client credentials | ✅ token request/caching | ✅ missing options | ✅ 500/invalid payload → Authentication | ✅ token via ct | ✅ 8-way single-flight | ✅ full OAuth round trip |
| Resilience | — (delegated to standard handler) | — | — | — | — | ✅ 503×2→200 retry proof |
| Pagination | ✅ cursor + page-number walks | ✅ eager arg validation | ✅ mid-stream 500, maxPages guard | ✅ mid-enumeration | n/a | ✅ 3-page cursor E2E |
| Response caching | ✅ hit/miss | — | ✅ non-200 not cached | inherited | IMemoryCache guarantees | — |
| Correlation | ✅ generated/explicit | — | ✅ disabled mode | — | — | ✅ server-side echo |
| Telemetry | ✅ span+metric tags | — | ✅ error status/tags | — | listener snapshots | — |
| DI & options | ✅ default/named/typed | ✅ validation failures | ✅ config binding | — | singleton factory | ✅ all E2E via DI |
| Download/multipart | ✅ round trips | ✅ null destination | ✅ 404 download | ✅ token honored | — | ✅ 128 KB stream + upload |
| MockApiHandler | ✅ matching/sequences | ✅ strict mode | ✅ unmatched 404 | — | ✅ locked recording | — |

Executed: 105 unit + 7 integration tests × 2 TFMs = **224 test runs**, all green.
