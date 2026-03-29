using System.Reflection;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using MealPlanner.Api.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace MealPlanner.Api.Infrastructure.Http;

public static class RequestValidationEndpointFilterFactory
{
    public static EndpointFilterDelegate Create(
        EndpointFilterFactoryContext context,
        EndpointFilterDelegate next)
    {
        var parameterDescriptors = context.MethodInfo
            .GetParameters()
            .Select((parameter, index) => CreateDescriptor(parameter, index))
            .OfType<ParameterDescriptor>()
            .ToArray();

        if (parameterDescriptors.Length == 0)
        {
            return next;
        }

        return async invocationContext =>
        {
            Dictionary<string, List<string>>? errors = null;

            foreach (var descriptor in parameterDescriptors)
            {
                if (invocationContext.HttpContext.RequestServices.GetService(descriptor.ParameterType) is not null)
                {
                    continue;
                }

                var argument = invocationContext.Arguments[descriptor.ArgumentIndex];
                if (argument is null)
                {
                    continue;
                }

                ValidateArgument(argument, ref errors);
            }

            if (errors is not null)
            {
                throw new RequestValidationException(
                    errors.ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value.Distinct().ToArray()));
            }

            return await next(invocationContext);
        };
    }

    private static ParameterDescriptor? CreateDescriptor(ParameterInfo parameter, int argumentIndex)
    {
        var parameterType = parameter.ParameterType;
        var effectiveType = Nullable.GetUnderlyingType(parameterType) ?? parameterType;

        if (parameter.GetCustomAttribute<FromServicesAttribute>() is not null
            || parameter.GetCustomAttribute<FromKeyedServicesAttribute>() is not null
            || parameter.GetCustomAttribute<AsParametersAttribute>() is not null
            || IsFrameworkType(effectiveType)
            || IsSimpleType(effectiveType))
        {
            return null;
        }

        return new ParameterDescriptor(argumentIndex, effectiveType);
    }

    private static void ValidateArgument(
        object argument,
        ref Dictionary<string, List<string>>? errors)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(argument);

        if (Validator.TryValidateObject(argument, validationContext, validationResults, validateAllProperties: true))
        {
            return;
        }

        errors ??= new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var validationResult in validationResults)
        {
            var memberNames = validationResult.MemberNames.Any()
                ? validationResult.MemberNames
                : [string.Empty];

            foreach (var memberName in memberNames)
            {
                var normalizedMemberName = NormalizeMemberName(memberName);
                if (!errors.TryGetValue(normalizedMemberName, out var messages))
                {
                    messages = [];
                    errors[normalizedMemberName] = messages;
                }

                messages.Add(validationResult.ErrorMessage ?? "The request is invalid.");
            }
        }
    }

    private static string NormalizeMemberName(string memberName)
    {
        if (string.IsNullOrWhiteSpace(memberName))
        {
            return string.Empty;
        }

        return string.Join(
            ".",
            memberName
                .Split('.', StringSplitOptions.RemoveEmptyEntries)
                .Select(JsonNamingPolicy.CamelCase.ConvertName));
    }

    private static bool IsFrameworkType(Type type)
    {
        return type == typeof(HttpContext)
            || type == typeof(HttpRequest)
            || type == typeof(HttpResponse)
            || type == typeof(ClaimsPrincipal)
            || type == typeof(CancellationToken)
            || typeof(IFormFile).IsAssignableFrom(type)
            || typeof(Stream).IsAssignableFrom(type);
    }

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(Guid)
            || type == typeof(DateTime)
            || type == typeof(DateOnly)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeOnly)
            || type == typeof(TimeSpan)
            || type == typeof(Uri);
    }

    private sealed record ParameterDescriptor(int ArgumentIndex, Type ParameterType);
}
