using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.WebApi.Common;
using FluentValidation;
using System.Text.Json;

namespace Ambev.DeveloperEvaluation.WebApi.Middleware;

/// <summary>
/// Middleware global de tratamento de exceções.
/// SRP: responsabilidade única de converter exceções em respostas HTTP padronizadas.
/// </summary>
public class ValidationExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ValidationExceptionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await WriteResponseAsync(context, StatusCodes.Status400BadRequest, "Falha de validação",
                ex.Errors.Select(e => (ValidationErrorDetail)e));
        }
        catch (KeyNotFoundException ex)
        {
            await WriteResponseAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await WriteResponseAsync(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (DomainException ex)
        {
            await WriteResponseAsync(context, StatusCodes.Status422UnprocessableEntity, ex.Message);
        }
    }

    private static Task WriteResponseAsync(
        HttpContext context,
        int statusCode,
        string message,
        IEnumerable<ValidationErrorDetail>? errors = null)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors ?? []
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
