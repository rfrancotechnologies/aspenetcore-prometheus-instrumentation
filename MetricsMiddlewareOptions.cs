using System.Collections.Generic;

namespace Com.RFranco.AspNetCore.Prometheus
{
    public class MetricsMiddlewareOptions
    {
        private static readonly List<string> DEFAULT_EXCLUDED_PATHS = new List<string>{"/swagger", "/health", "/metrics"};

        public List<string> ExcludedPaths {get;set;} = DEFAULT_EXCLUDED_PATHS;
    }
}