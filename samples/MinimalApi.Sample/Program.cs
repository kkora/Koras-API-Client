// Minimal API sample: a small proxy over an upstream REST API using a typed client,
// configuration-bound options, and ApiError -> HTTP result mapping.
using Koras.ApiClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddKorasApiClient("placeholder", builder.Configuration.GetSection("PlaceholderApi"))
    .AddTypedClient<TodoGateway>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    message = "Koras.ApiClient Minimal API sample",
    endpoints = new[] { "/todos/1", "/todos/999999 (upstream 404)", "/todos" },
}));

app.MapGet("/todos/{id:int}", async (int id, TodoGateway gateway, CancellationToken cancellationToken) =>
{
    var response = await gateway.GetTodoAsync(id, cancellationToken);
    return response.Match<IResult>(
        todo => Results.Ok(todo),
        error => error.Category switch
        {
            ApiErrorCategory.NotFound => Results.NotFound(),
            ApiErrorCategory.Validation => Results.Problem(title: error.Message, statusCode: StatusCodes.Status400BadRequest),
            _ when error.IsTransient => Results.Problem(title: "Upstream API unavailable", statusCode: StatusCodes.Status503ServiceUnavailable),
            _ => Results.Problem(title: error.Message, statusCode: StatusCodes.Status502BadGateway),
        });
});

app.MapGet("/todos", async (TodoGateway gateway, CancellationToken cancellationToken) =>
{
    var response = await gateway.GetTodosAsync(cancellationToken);
    return response.Match<IResult>(
        todos => Results.Ok(todos),
        error => Results.Problem(title: error.Message, statusCode: StatusCodes.Status502BadGateway));
});

app.Run();

internal sealed class TodoGateway(IApiClient client)
{
    public Task<ApiResponse<Todo>> GetTodoAsync(int id, CancellationToken cancellationToken) =>
        client.Get($"todos/{id}").SendAsync<Todo>(cancellationToken);

    public Task<ApiResponse<List<Todo>>> GetTodosAsync(CancellationToken cancellationToken) =>
        client.Get("todos").WithQuery("_limit", 10).WithCache(TimeSpan.FromSeconds(30)).SendAsync<List<Todo>>(cancellationToken);
}

internal sealed record Todo(int Id, int UserId, string Title, bool Completed);
