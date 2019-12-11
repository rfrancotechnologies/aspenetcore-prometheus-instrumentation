using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Prometheus;

namespace Com.RFranco.AspNetCore.Prometheus
{

    public class MetricsMiddleware
    {
        private MetricsMiddlewareOptions _options;
        private const string UNKNOWN_REQUEST_PATH = "NotFound";
        private readonly RequestDelegate _next;
        private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
        private static readonly Counter ErrorRequestsProcessed = Metrics.CreateCounter("server_request_error_total", "Number of unsuccessfull processed requests.", "method", "code_error");
        private static readonly Gauge OngoingRequests = Metrics.CreateGauge("server_request_in_progress", "Number of ongoing requests.", "method");
        private static readonly Histogram RequestResponseHistogram = Metrics.CreateHistogram("server_request_duration_seconds", "Histogram of request duration in seconds.", "method");

        public MetricsMiddleware(RequestDelegate next, IActionDescriptorCollectionProvider actionDescriptorCollectionProvider, MetricsMiddlewareOptions options)
        {
            _next = next;
            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
            _options = options;
        }

        public async Task Invoke(HttpContext context)
        {

            if (!MustBeObserved(context)) await _next(context);
            else
            {
                var fullMethodName = $"{context.Request.Method} {GetRequestTemplate(context.Request)}";

                OngoingRequests.Labels(fullMethodName).Inc();

                using (RequestResponseHistogram.Labels(fullMethodName).NewTimer())
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
                    }
                }
            }

        }

        private bool MustBeObserved(HttpContext context)
        {
            return _options.ExcludedPaths.Where(path => context.Request.GetDisplayUrl().IndexOf(path) != -1).Count() == 0;
        }
        private string GetRequestTemplate(HttpRequest request)
        {
            string requestTemplate = UNKNOWN_REQUEST_PATH;

            var routes = _actionDescriptorCollectionProvider.ActionDescriptors.Items;
            foreach (ActionDescriptor descriptor in routes)
            {
                var currentTemplate = $"/{descriptor.AttributeRouteInfo.Template}";
                var template = TemplateParser.Parse(currentTemplate);
                var matcher = new TemplateMatcher(template, GetDefaults(template));
                if (matcher.TryMatch(request.Path.Value, new RouteValueDictionary()))
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
        public static IApplicationBuilder UseMetricsMiddleware(this IApplicationBuilder builder, MetricsMiddlewareOptions options = null)
        {
            return builder.UseMiddleware<MetricsMiddleware>(options ?? new MetricsMiddlewareOptions());
        }
    }
}