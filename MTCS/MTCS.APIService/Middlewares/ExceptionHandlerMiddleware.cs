using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using MTCS.Data.Response;
using System.Net;

namespace MTCS.APIService.Middlewares
{
    public class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlerMiddleware> _logger;

        public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FULL EXCEPTION: {ExMessage}, Inner: {InnerMessage}",
            ex.Message,
            ex.InnerException?.Message ?? "No inner exception");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            var statusCode = GetStatusCode(exception);
            context.Response.StatusCode = (int)statusCode;

            var response = CreateApiResponse(exception, statusCode);
            await context.Response.WriteAsJsonAsync(response);
        }

        private static HttpStatusCode GetStatusCode(Exception exception)
        {
            return exception switch
            {
                // EF Core exceptions - check these first since they're most specific
                DbUpdateConcurrencyException => HttpStatusCode.Conflict,
                DbUpdateException => HttpStatusCode.BadRequest,

                // Repository exceptions
                KeyNotFoundException => HttpStatusCode.NotFound,
                ArgumentException => HttpStatusCode.BadRequest,
                InvalidOperationException => HttpStatusCode.BadRequest,

                // Service exceptions
                UnauthorizedAccessException => HttpStatusCode.Unauthorized,

                // Add more specific exception mappings as needed

                // Default for any other exceptions
                _ => HttpStatusCode.InternalServerError
            };
        }

        private static ApiResponse<object> CreateApiResponse(Exception exception, HttpStatusCode statusCode)
        {
            var errorMessage = exception.Message;
            var errors = new List<string> { errorMessage };

            if (exception.InnerException != null)
            {
                errors.Add(exception.InnerException.Message);

                if (exception is DbUpdateException dbEx && dbEx.InnerException?.InnerException != null)
                {
                    errors.Add(dbEx.InnerException.InnerException.Message);
                }
            }

            if (exception is AggregateException aggregateException)
            {
                errors.AddRange(aggregateException.InnerExceptions.Select(ex => ex.Message));
            }

            var message = statusCode switch
            {
                HttpStatusCode.NotFound => "Data not found",
                HttpStatusCode.BadRequest => "Bad request",
                HttpStatusCode.Unauthorized => "Unauthorized",
                HttpStatusCode.InternalServerError => "An unexpected error occurred",
                _ => "An error occurred"
            };

            string errorString = string.Join(", ", errors);

            return new ApiResponse<object>(false, null, message, null, errorString);
        }
    }

    public class ValidateModelAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = string.Join("; ", context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                var response = new ApiResponse<object>(
                    false,
                    null,
                    "Validation failed",
                    null,
                    errors);

                context.Result = new BadRequestObjectResult(response);
            }
        }
    }
}
