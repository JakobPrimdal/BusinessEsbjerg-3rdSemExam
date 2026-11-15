using System.Text.Json;
using Api;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Service.Exceptions;
 
namespace UnitTests;
 
public class TestGlobalExceptionHandler
{
    [Theory]
    [InlineData(typeof(NotFoundException), StatusCodes.Status404NotFound, "boom")]
    [InlineData(typeof(BadRequestException), StatusCodes.Status400BadRequest, "boom")]
    [InlineData(typeof(UnauthorizedException), StatusCodes.Status401Unauthorized, "boom")]
    [InlineData(typeof(InvalidOperationException), StatusCodes.Status500InternalServerError, "An unexpected error occurred.")]
    public async Task TryHandleAsync_MapsExceptionToExpectedStatusAndDetail(Type exceptionType, int expectedStatus, string expectedDetail)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "boom")!;
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        context.Response.Body = new MemoryStream();
 
        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);
 
        Assert.True(handled);
        Assert.Equal(expectedStatus, context.Response.StatusCode);
 
        context.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(expectedStatus, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(expectedDetail, json.RootElement.GetProperty("detail").GetString());
    }
}