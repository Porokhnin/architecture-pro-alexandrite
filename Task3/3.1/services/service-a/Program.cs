using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace service_a
{
    public class Program
    {
        private static readonly ActivitySource source = new ActivitySource("service-a", "1.0.0");

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddHttpClient("client-b", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["SERVICE_B_URL"] ?? "http://localhost:5002");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(10);
            });

            builder.Services.AddOpenTelemetry()
                .WithTracing(tracerProviderBuilder =>
                {
                    tracerProviderBuilder
                        .AddSource(source.Name) // Replace with your ActivitySource name
                        .SetResourceBuilder(
                            ResourceBuilder.CreateDefault()
                                .AddService(serviceName: builder.Environment.ApplicationName))
                        .AddAspNetCoreInstrumentation() // For ASP.NET Core requests
                        .AddHttpClientInstrumentation() // For HTTP client calls
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


            app.MapGet("/tracing", async (HttpContext httpContext, IHttpClientFactory httpClientFactory) =>
            {
                var client = httpClientFactory.CreateClient("client-b");

                SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(1));

                var result = await client.GetFromJsonAsync<object>("tracing");

                SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(1));

                return Results.Ok(result);
            })
            .WithName("Tracing")
            .WithOpenApi();

            app.Run();
        }
    }
}
