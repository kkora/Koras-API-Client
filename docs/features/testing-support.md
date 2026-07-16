# Testing Support (Koras.ApiClient.Testing)

## Overview

`Koras.ApiClient.Testing` lets consumers unit-test API integrations without a network — and without hand-mocking `HttpMessageHandler`. Tests run through the **real** client pipeline (URI composition, serialization, correlation, error normalization), so they catch real bugs.

## Installation

```bash
dotnet add package Koras.ApiClient.Testing   # test projects only
```

Framework-agnostic: xUnit, NUnit, MSTest all work.

## Basic usage

```csharp
var handler = new MockApiHandler();
handler.When(HttpMethod.Get, "/orders/42").RespondWithJson(new Order("42", "open"));
handler.When(HttpMethod.Get, "/orders/*").RespondWith(HttpStatusCode.NotFound);   // prefix wildcard

IApiClient client = KorasApiClientTestFactory.Create(handler);

var found = await client.Get("orders/42").SendAsync<Order>();
Assert.True(found.IsSuccess);
```

## Scripting failures

```csharp
handler.When(HttpMethod.Get, "/flaky")
    .RespondWith(HttpStatusCode.ServiceUnavailable)      // 1st call
    .RespondWithJson(new Widget(1, "ok"));               // 2nd call onward (last repeats)

handler.When(HttpMethod.Get, "/down").Throws(new HttpRequestException("refused"));
handler.When(HttpMethod.Post, "/invalid").RespondWithProblem(
    new ProblemDocument { Title = "Validation failed", Errors = new Dictionary<string, string[]> { ["name"] = ["Required"] } });
handler.When(HttpMethod.Get, "/slow").RespondWith(HttpStatusCode.TooManyRequests,
    configure: r => r.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30)));
```

Full control: `RespondWith(() => new HttpResponseMessage(...))`.

## Asserting on requests

```csharp
var request = Assert.Single(handler.Requests);
Assert.Equal(HttpMethod.Post, request.Method);
Assert.Contains("\"name\":\"gadget\"", request.Body);
Assert.Equal("Bearer test", request.Headers["Authorization"][0]);
Assert.Equal("?page=2", request.Uri.Query);
```

Strict mode: `handler.ThrowOnUnmatchedRequest = true` turns unexpected calls into test failures instead of 404s.

## Testing DI-registered clients

Swap only the transport; everything else (auth handlers, caching, resilience, options) stays real:

```csharp
services.AddKorasApiClient("vendor", o => o.BaseAddress = new Uri("https://mock.local/"))
    .WithApiKey(k => k.ApiKey = "test-key")
    .HttpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => handler);
```

## Matching rules

Patterns match the absolute path case-insensitively; include `?` to match path-and-query exactly; end with `*` for prefix matching. First registered match wins.

## Common mistakes

- Mocking `IApiClient` itself with a mocking library — you lose serialization/error-pipeline coverage; prefer the real client over `MockApiHandler`.
- Forgetting the leading path segment when the client uses a base address with a path (`https://host/v1/` + `orders` → pattern `/v1/orders`).
