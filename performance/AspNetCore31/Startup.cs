using System.Collections.Generic;
using System.Reflection;
using Datadog.RuntimeMetrics;
using Datadog.RuntimeMetrics.Hosting;
using Datadog.Trace;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AspNetCore31
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // configure options from default sources (e.g. env vars)
            services.Configure<StatsdConnectionOptions>(Configuration);

            // register the global Tracer
            services.AddDatadogTracing(Tracer.Instance);

            // register the services required to collect metrics and send them to dogstatsd
            services.AddDatadogRuntimeMetrics(options => options.Tags = GetMetricsTags());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, Tracer tracer)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            int maxSpans = Configuration.GetValue("DD_MAX_MANUAL_SPANS", 0);

            if (maxSpans > 0)
            {
                // hack: internal method
                tracer.GetType()
                      .GetMethod("StartDiagnosticObservers", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                     ?.Invoke(tracer, null);

                int maxTags = Configuration.GetValue("DD_MAX_TAGS", 0);

                // first span is created with DiagnosticSource,
                // additional spans are created manually in middleware
                app.UseDatadogTracing(tracer, maxSpans - 1, maxTags);
            }

            app.Run(async context =>
                    {
                        context.Response.ContentType = "text/plain";
                        await context.Response.WriteAsync("Hello, world!");
                    });
        }

        private IEnumerable<string> GetMetricsTags()
        {
            int maxSpans = Configuration.GetValue("DD_MAX_MANUAL_SPANS", 0);
            int maxTags = Configuration.GetValue("DD_MAX_TAGS", 0);
            string tracerVersion = Configuration["DD_TRACER_VERSION"];
            string messagePackVersion = typeof(MessagePack.IFormatterResolver).Assembly.GetName().Version?.ToString() ?? "none";

            var tags = new List<string>(6);
            tags.Add($"service_name:{this.GetType().Namespace}");
            tags.Add($"max_spans:{maxSpans}");
            tags.Add($"max_tags:{maxTags}");
            tags.Add($"messagepack_version:${messagePackVersion}");

            if (maxSpans == 0)
            {
                tags.Add("tracer_mode:none");
                tags.Add("tracer_version:none");
            }
            else
            {
                tags.Add("tracer_mode:manual");
                tags.Add($"tracer_version:{tracerVersion}");
            }

            return tags;
        }
    }
}