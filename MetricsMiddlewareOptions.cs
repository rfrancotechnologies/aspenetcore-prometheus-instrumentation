using System.Collections.Generic;

namespace Com.RFranco.AspNetCore.Prometheus
{
    public class MetricsMiddlewareOptions
    {
        private static readonly List<string> DEFAULT_EXCLUDED_PATHS = new List<string>{"/swagger", "/health", "/metrics"};

        private static readonly double[] DefaultBuckets = {.1, .25, 1, 2.5, 5 };

        public List<string> ExcludedPaths {get;set;} = DEFAULT_EXCLUDED_PATHS;

        public double[] Buckets {get; set;} = DefaultBuckets;
    }
}