using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace service_b
{
    public class Program
    {
        private static ActivitySource source = new ActivitySource("service-b", "1.0.0");

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();


            builder.Services.AddOpenTelemetry()
                .WithTracing(tracerProviderBuilder =>
                {
                    tracerProviderBuilder
                        .AddSource(source.Name) // Replace with your ActivitySource name
                        .SetResourceBuilder(
                            ResourceBuilder.CreateDefault()
                                .AddService(serviceName: builder.Environment.ApplicationName))
                        .AddAspNetCoreInstrumentation() // For ASP.NET Core requests
                        .AddOtlpExporter(otlpOptions =>
                        {
                            otlpOptions.Endpoint = new Uri(builder.Configuration["OTLP_URL"] ?? "http://localhost:4317"); // Jaeger OTLP endpoint
                        });
                });


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }


            app.MapGet("/tracing", async (HttpContext httpContext) =>
            {
                SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(2));

                return Results.Ok(new object());
            })
            .WithName("Tracing")
            .WithOpenApi();

            app.Run();
        }
    }
}
