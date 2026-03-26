namespace MealPlanner.Api.Infrastructure.Http;

using System.Text.Json;
using MealPlanner.Api.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

public sealed class GlobalExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlingMiddleware> logger,
    IHostEnvironment hostEnvironment)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (Exception exception)
        {
            if (httpContext.Response.HasStarted)
            {
                logger.LogError(
                    exception,
                    "Unhandled exception after the response started for {Method} {Path}",
                    httpContext.Request.Method,
                    httpContext.Request.Path);

                throw;
            }

            await HandleExceptionAsync(httpContext, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
    {
        var traceId = httpContext.TraceIdentifier;

        var problemDetails = exception switch
        {
            NotFoundException notFoundException => CreateProblemDetails(
                statusCode: StatusCodes.Status404NotFound,
                title: "Resource not found",
                detail: notFoundException.Message,
                type: "https://httpstatuses.com/404",
                traceId: traceId),
            RequestValidationException validationException => CreateValidationProblemDetails(
                validationException,
                traceId),
            UnauthorizedAccessException unauthorizedAccessException => CreateProblemDetails(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: unauthorizedAccessException.Message,
                type: "https://httpstatuses.com/401",
                traceId: traceId),
            _ => CreateProblemDetails(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                detail: hostEnvironment.IsDevelopment() ? exception.Message : null,
                type: "https://httpstatuses.com/500",
                traceId: traceId),
        };

        LogException(httpContext, exception, problemDetails.Status ?? StatusCodes.Status500InternalServerError);

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            problemDetails,
            problemDetails.GetType(),
            cancellationToken: httpContext.RequestAborted);
    }

    private ProblemDetails CreateProblemDetails(
        int statusCode,
        string title,
        string? detail,
        string type,
        string traceId)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type,
            Extensions =
            {
                ["traceId"] = traceId,
            },
        };
    }

    private ValidationProblemDetails CreateValidationProblemDetails(
        RequestValidationException validationException,
        string traceId)
    {
        var validationProblemDetails = new ValidationProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Detail = validationException.Message,
            Type = "https://httpstatuses.com/400",
        };

        foreach (var (key, value) in validationException.Errors)
        {
            validationProblemDetails.Errors.Add(key, value);
        }

        validationProblemDetails.Extensions["traceId"] = traceId;
        return validationProblemDetails;
    }

    private void LogException(HttpContext httpContext, Exception exception, int statusCode)
    {
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path}. Responding with {StatusCode}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                statusCode);

            return;
        }

        logger.LogWarning(
            exception,
            "Request failed for {Method} {Path}. Responding with {StatusCode}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            statusCode);
    }
}
