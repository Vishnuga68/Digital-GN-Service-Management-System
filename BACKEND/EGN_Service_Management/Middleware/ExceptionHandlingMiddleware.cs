using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using EGN_Service_Management.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EGN_Service_Management.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
                _logger.LogError(ex, "An unhandled exception occurred during request execution: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            var statusCode = HttpStatusCode.InternalServerError;
            var message = "An unexpected error occurred on the server.";
            List<string>? errors = null;

            switch (exception)
            {
                case KeyNotFoundException keyNotFoundEx:
                    statusCode = HttpStatusCode.NotFound;
                    message = keyNotFoundEx.Message;
                    break;

                case UnauthorizedAccessException unauthorizedEx:
                    statusCode = HttpStatusCode.Unauthorized;
                    message = unauthorizedEx.Message;
                    break;

                case ArgumentException argumentEx:
                    statusCode = HttpStatusCode.BadRequest;
                    message = argumentEx.Message;
                    break;

                case InvalidOperationException invalidOpEx:
                    statusCode = HttpStatusCode.Conflict;
                    message = invalidOpEx.Message;
                    break;

                default:
                    // Under production, we avoid exposing raw stack trace but for development, we can show exception type
                    message = exception.Message;
                    break;
            }

            context.Response.StatusCode = (int)statusCode;

            var responseEnvelope = ApiResponse<object>.ErrorResponse(message, errors);
            
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var jsonString = JsonSerializer.Serialize(responseEnvelope, options);
            return context.Response.WriteAsync(jsonString);
        }
    }
}
