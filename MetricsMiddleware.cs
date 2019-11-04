using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Prometheus;

namespace Com.RFranco.Iris.AspNetCore.Prometheus
{

    public class MetricsMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly Counter RequestsProcessed = Metrics.CreateCounter("total_successfull_requests", "Number of successfull processed requests.", "method");
        private static readonly Counter ErrorRequestsProcessed = Metrics.CreateCounter("total_error_responses", "Number of unsuccessfull processed requests.", "method", "code_error");
        private static readonly Gauge OngoingRequests = Metrics.CreateGauge("requests_in_progress", "Number of ongoing requests.", "method");
        private static readonly Summary RequestsDurationSummaryInSeconds = Metrics.CreateSummary("requests_duration_summary_seconds", "A Summary of request duration (in seconds) over last 10 minutes.", "method");
        private static readonly Histogram RequestResponseHistogram = Metrics.CreateHistogram("requests_duration_histogram_seconds", "Histogram of request duration in seconds.", "method");

        
        public MetricsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        
        public async Task Invoke(HttpContext context)
        {
            var fullMethodName = $"{context.Request.Method} {context.Request.Path.Value}";
            OngoingRequests.Labels(fullMethodName).Inc();

            using (RequestResponseHistogram.Labels(fullMethodName).NewTimer())
            using (RequestsDurationSummaryInSeconds.Labels(fullMethodName).NewTimer())
            {
                try
                {
                    await _next(context);
                }
                catch (Exception)
                {
                    ErrorRequestsProcessed.Labels(fullMethodName, context.Response.StatusCode.ToString()).Inc();
                    throw;
                }
                finally
                {
                    OngoingRequests.Labels(fullMethodName).Dec();
                    RequestsProcessed.Labels(fullMethodName).Inc();
                }
            }
        }
    }

    public static class MetricsMiddlewareExtensions
    {
        public static IApplicationBuilder UseMetricsMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MetricsMiddleware>();
        }
    }
}