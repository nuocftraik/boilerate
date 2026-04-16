using Boilerate.Application.Common.Exceptions;
using Boilerate.Application.Common.Interfaces;
using Boilerate.Domain.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;
using System.Net;

namespace Boilerate.Infrastructure.Middleware;

/// <summary>
/// Middleware để catch và handle tất cả exceptions
/// Phải đặt đầu tiên trong middleware pipeline
/// </summary>
internal class ExceptionMiddleware : IMiddleware
{
    private readonly ICurrentUser _currentUser;
    private readonly ISerializerService _jsonSerializer;

    public ExceptionMiddleware(
        ICurrentUser currentUser,
        ISerializerService jsonSerializer)
    {
        _currentUser = currentUser;
        _jsonSerializer = jsonSerializer;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            // Continue với request pipeline
            await next(context);
        }
        catch (Exception ex)
        {
            var exception = ex; // work on a local reference

            // 1. Lấy user context
            var email = _currentUser.GetUserEmail() ?? "Anonymous";
            var userId = _currentUser.GetUserId();

            // 2. Push context vào Serilog (optional scoped properties)
            using (LogContext.PushProperty("UserId", userId))
            using (LogContext.PushProperty("UserEmail", email))
            {
                // 3. Generate unique error ID
                var errorId = Guid.NewGuid().ToString();
                using (LogContext.PushProperty("ErrorId", errorId))
                using (LogContext.PushProperty("StackTrace", exception.StackTrace))
                {
                    // 4. Tạo ErrorResult
                    var errorResult = new ErrorResult
                    {
                        Source = exception.TargetSite?.DeclaringType?.FullName,
                        Exception = exception.Message?.Trim(),
                        ErrorId = errorId,
                        SupportMessage = $"Provide the ErrorId {errorId} to the support team for further analysis."
                    };

                    // 5. Unwrap inner exceptions for root cause (except custom exceptions)
                    if (exception is not CustomException && exception is not DomainException && exception.InnerException != null)
                    {
                        while (exception.InnerException != null)
                        {
                            exception = exception.InnerException;
                        }

                        errorResult.Exception = exception.Message?.Trim();
                    }

                    // 6. Handle FluentValidation exceptions
                    if (exception is FluentValidation.ValidationException fluentException)
                    {
                        errorResult.Exception = "One or More Validations failed.";
                        foreach (var error in fluentException.Errors)
                        {
                            errorResult.Messages.Add(error.ErrorMessage);
                        }
                    }

                    // 7. Map status code based on exception type
                    switch (exception)
                    {
                        case CustomException ce:
                            errorResult.StatusCode = (int)ce.StatusCode;
                            if (ce.ErrorMessages is not null)
                            {
                                errorResult.Messages = ce.ErrorMessages;
                            }
                            break;

                        case DomainException de:
                            errorResult.StatusCode = (int)de.StatusCode;
                            if (de.ErrorMessages is not null)
                            {
                                errorResult.Messages = de.ErrorMessages;
                            }
                            break;

                        case KeyNotFoundException:
                            errorResult.StatusCode = (int)HttpStatusCode.NotFound;
                            break;

                        case FluentValidation.ValidationException:
                            errorResult.StatusCode = (int)HttpStatusCode.BadRequest;
                            break;

                        default:
                            errorResult.StatusCode = (int)HttpStatusCode.InternalServerError;
                            break;
                    }

                    // 8. Log error
                    Log.Error("{ExceptionMessage} Request failed with Status Code {StatusCode} and Error Id {ErrorId}.",
                        errorResult.Exception, errorResult.StatusCode, errorId);

                    // 9. Write error response (if possible)
                    var response = context.Response;
                    if (!response.HasStarted)
                    {
                        response.ContentType = "application/json";
                        response.StatusCode = errorResult.StatusCode;
                        await response.WriteAsync(_jsonSerializer.Serialize(errorResult));
                    }
                    else
                    {
                        Log.Warning("Can't write error response. Response has already started.");
                    }
                }
            }
        }
    }
}
