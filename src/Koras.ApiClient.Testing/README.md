# Koras.ApiClient.Testing

Test doubles for [Koras.ApiClient](https://github.com/KKORA/koras-api-client): unit-test your API integrations without a network, exercising the framework's **real** serialization and error pipeline.

```csharp
var handler = new MockApiHandler();
handler.When(HttpMethod.Get, "/todos/1")
       .RespondWithJson(new Todo(1, "write tests", Done: false));
handler.When(HttpMethod.Get, "/todos/404")
       .RespondWithProblem(new ProblemDocument { Title = "Not found" }, HttpStatusCode.NotFound);

IApiClient client = KorasApiClientTestFactory.Create(handler);

var ok = await client.Get("todos/1").SendAsync<Todo>();
Assert.True(ok.IsSuccess);

var missing = await client.Get("todos/404").SendAsync<Todo>();
Assert.Equal(ApiErrorCategory.NotFound, missing.Error!.Category);

// Every request is recorded — assert on method, URI, headers, and body:
Assert.Equal(2, handler.Requests.Count);
```

- **Sequences** — chain `RespondWith` calls to script flaky endpoints (`.RespondWith(HttpStatusCode.ServiceUnavailable).RespondWithJson(ok)`).
- **Exceptions** — `Throws(new HttpRequestException())` simulates network failures.
- **Framework-agnostic** — works with xUnit, NUnit, and MSTest.

Documentation: https://github.com/KKORA/koras-api-client/tree/main/docs
