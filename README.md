# aspenetcore-prometheus-instrumentation

A veri simple library to instrumentalize your Asp.Net core applications with Prometheus.

## Usage

You should add this middleware on application builder so that we get our custom metrics:

```csharp
public void Configure(IApplicationBuilder app, IHostingEnvironment env)  
{  
    // others ...  
  
    app.UseMetricServer();  
  
    app.UseMetricsMiddleware();  
}  

```

Its also possible to configure the middleware overriding the default behavior (ignoring calls that contains`/swagger`, `/health` and `/metrics` in the path) passing a list of path to be ignored as parameters (in the next example, we are excluding calls that contains `/swagger` in the path):

```csharp

public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
{
    ...
    app.UseMetricsMiddleware(new MetricsMiddlewareOptions{ExcludedPaths = new List<string>{"/swagger"}});
    ...
}
```

Also is possible to customize the buckets of the internal histogram used:

```csharp

public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
{
    ...
    app.UseMetricsMiddleware(new MetricsMiddlewareOptions{Buckets = new double[]{ .005, .01, .025, .05, .075, .1, .25, .5, .75, 1, 2.5, 5, 7.5, 10 }});
    ...
}
```

By default, it uses the next list of buckets `{ .1, .25, 1, 2.5, 5 }`.
