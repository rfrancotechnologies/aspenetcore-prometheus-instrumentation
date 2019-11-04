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
