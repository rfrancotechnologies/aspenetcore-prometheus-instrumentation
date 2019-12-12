using System.Collections.Generic;

namespace Com.RFranco.AspNetCore.Prometheus
{
    public class MetricsMiddlewareOptions
    {
        private static readonly List<string> DEFAULT_EXCLUDED_PATHS = new List<string>{"/swagger", "/health", "/metrics"};

        private static readonly double[] DefaultBuckets = { .005, .01, .025, .05, .075, .1, .25, .5, .75, 1, 2.5, 5, 7.5, 10 };

        public List<string> ExcludedPaths {get;set;} = DEFAULT_EXCLUDED_PATHS;

        public double[] Buckets {get; set;} = DefaultBuckets;
    }
}