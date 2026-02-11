using Assesment.Domain;
using Assesment.Infrastructure;
using System.Text.Json;

namespace Assesment.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IExceptionJournalRepository journalRepository)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, journalRepository);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, IExceptionJournalRepository journalRepository)
    {
        var eventId = DateTime.UtcNow.Ticks;
        var isSecureException = exception is SecureException;

        // Get query parameters
        var queryParams = context.Request.Query
            .ToDictionary(q => q.Key, q => q.Value.ToString());
        var queryParamsJson = JsonSerializer.Serialize(queryParams);

        // Get body parameters (if available)
        string bodyParams = "{}";
        if (context.Request.ContentLength > 0)
        {
            context.Request.Body.Position = 0;
            using var reader = new StreamReader(context.Request.Body);
            bodyParams = await reader.ReadToEndAsync();
        }

        // Create journal entry
        var journal = new ExceptionJournal
        {
            EventId = eventId,
            ExceptionType = exception.GetType().Name,
            Message = exception.Message,
            StackTrace = exception.StackTrace ?? string.Empty,
            QueryParameters = queryParamsJson,
            BodyParameters = bodyParams,
            Endpoint = $"{context.Request.Method} {context.Request.Path}"
        };

        try
        {
            eventId = await journalRepository.CreateAsync(journal, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write to exception journal");
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var response = isSecureException
            ? new
            {
                type = "Secure",
                id = eventId.ToString(),
                data = new { message = exception.Message }
            }
            : new
            {
                type = "Exception",
                id = eventId.ToString(),
                data = new { message = $"Internal server error ID = {eventId}" }
            };

        await context.Response.WriteAsJsonAsync(response);
    }
}
