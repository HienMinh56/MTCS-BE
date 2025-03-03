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
                _logger.LogError(ex, "Unhandled exception");
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

            // For aggregate exceptions, collect all inner exceptions
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

            return new ApiResponse<object>(false, null, message, errorString);
        }
    }
}
