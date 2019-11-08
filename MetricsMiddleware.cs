using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Prometheus;

namespace Com.RFranco.AspNetCore.Prometheus
{

    public class MetricsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
        private static readonly Counter RequestsProcessed = Metrics.CreateCounter("http_requests_total", "Number of successfull processed requests.", "method");
        private static readonly Counter ErrorRequestsProcessed = Metrics.CreateCounter("http_error_total", "Number of unsuccessfull processed requests.", "method", "code_error");
        private static readonly Gauge OngoingRequests = Metrics.CreateGauge("http_requests_in_progress", "Number of ongoing requests.", "method");
        private static readonly Summary RequestsDurationSummaryInSeconds = Metrics.CreateSummary("http_requests_duration_summary_seconds", "A Summary of request duration (in seconds) over last 10 minutes.", "method");
        private static readonly Histogram RequestResponseHistogram = Metrics.CreateHistogram("http_requests_duration_histogram_seconds", "Histogram of request duration in seconds.", "method");

        public MetricsMiddleware(RequestDelegate next, IActionDescriptorCollectionProvider actionDescriptorCollectionProvider)
        {
            _next = next;
            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
        }

        public async Task Invoke(HttpContext context)
        {
            var fullMethodName = $"{context.Request.Method} {GetRequestTemplate(context.Request)}";
            
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

                    if (context.Response.StatusCode != StatusCodes.Status200OK)
                        ErrorRequestsProcessed.Labels(fullMethodName, context.Response.StatusCode.ToString()).Inc();

                    RequestsProcessed.Labels(fullMethodName).Inc();
                }
            }
        }

        private string GetRequestTemplate(HttpRequest request)
        {
            string requestTemplate = request.Path.Value;

            var routes = _actionDescriptorCollectionProvider.ActionDescriptors.Items;
            foreach (ActionDescriptor descriptor in routes)
            {
                var currentTemplate = $"/{descriptor.AttributeRouteInfo.Template}";
                var template = TemplateParser.Parse(currentTemplate);
                var matcher = new TemplateMatcher(template, GetDefaults(template));
                if(matcher.TryMatch(request.Path.Value, new RouteValueDictionary())) 
                {
                    requestTemplate = currentTemplate;
                    break;
                }
            }
            return requestTemplate;
        }

        private RouteValueDictionary GetDefaults(RouteTemplate parsedTemplate)
        {
            var result = new RouteValueDictionary();
            foreach (var parameter in parsedTemplate.Parameters)
            {
                if (parameter.DefaultValue != null)
                {
                    result.Add(parameter.Name, parameter.DefaultValue);
                }
            }
            return result;
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