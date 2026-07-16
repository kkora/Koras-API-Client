using System.Net;
using Koras.ApiClient.UnitTests.Support;
using Xunit;

namespace Koras.ApiClient.UnitTests;

public class ApiResponseTests
{
    private static ApiError SampleError => new()
    {
        Category = ApiErrorCategory.NotFound,
        Code = "http_404",
        Message = "missing",
        StatusCode = HttpStatusCode.NotFound,
    };

    [Fact]
    public void IsSuccess_TrueWhenNoError()
    {
        var response = new ApiResponse { StatusCode = HttpStatusCode.OK };
        Assert.True(response.IsSuccess);
        response.EnsureSuccess();
    }

    [Fact]
    public void EnsureSuccess_ThrowsApiClientException_WithError()
    {
        var response = new ApiResponse { StatusCode = HttpStatusCode.NotFound, Error = SampleError };
        var exception = Assert.Throws<ApiClientException>(response.EnsureSuccess);
        Assert.Same(response.Error, exception.Error);
        Assert.Equal("missing", exception.Message);
    }

    [Fact]
    public void GetValueOrThrow_ReturnsValue_OnSuccess()
    {
        var response = new ApiResponse<Widget> { StatusCode = HttpStatusCode.OK, Value = new Widget(1, "a") };
        Assert.Equal(new Widget(1, "a"), response.GetValueOrThrow());
    }

    [Fact]
    public void GetValueOrThrow_Throws_OnFailure()
    {
        var response = new ApiResponse<Widget> { StatusCode = HttpStatusCode.NotFound, Error = SampleError };
        Assert.Throws<ApiClientException>(() => response.GetValueOrThrow());
    }

    [Fact]
    public void GetValueOrThrow_Throws_OnSuccessWithoutValue()
    {
        var response = new ApiResponse<Widget> { StatusCode = HttpStatusCode.NoContent };
        var exception = Assert.Throws<ApiClientException>(() => response.GetValueOrThrow());
        Assert.Equal("empty_response", exception.Error.Code);
    }

    [Fact]
    public void Match_InvokesCorrectBranch()
    {
        var success = new ApiResponse<Widget> { StatusCode = HttpStatusCode.OK, Value = new Widget(1, "a") };
        var failure = new ApiResponse<Widget> { StatusCode = HttpStatusCode.NotFound, Error = SampleError };

        Assert.Equal("a", success.Match(value => value!.Name, error => error.Code));
        Assert.Equal("http_404", failure.Match(value => value!.Name, error => error.Code));
    }

    [Fact]
    public void ApiError_IsTransient_FollowsCategoryTable()
    {
        Assert.True((SampleError with { Category = ApiErrorCategory.Network }).IsTransient);
        Assert.True((SampleError with { Category = ApiErrorCategory.Timeout }).IsTransient);
        Assert.True((SampleError with { Category = ApiErrorCategory.RateLimited }).IsTransient);
        Assert.True((SampleError with { Category = ApiErrorCategory.Server }).IsTransient);
        Assert.False((SampleError with { Category = ApiErrorCategory.Validation }).IsTransient);
        Assert.False((SampleError with { Category = ApiErrorCategory.NotFound }).IsTransient);
        Assert.False((SampleError with { Category = ApiErrorCategory.Authentication }).IsTransient);
    }

    [Fact]
    public void ApiClientException_RejectsNullError()
    {
        Assert.Throws<ArgumentNullException>(() => new ApiClientException(null!));
    }
}
