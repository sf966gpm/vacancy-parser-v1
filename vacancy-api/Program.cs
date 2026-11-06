using System.Text.Json.Serialization;
using Vacancy.Api.Messaging.RabbitMq;
using Vacancy.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

builder.Services.AddSingleton<HhJobPublisher>();
builder.Services.AddSingleton<IHhJobPublisher>(services =>
    services.GetRequiredService<HhJobPublisher>());
builder.Services.AddHostedService(services =>
    services.GetRequiredService<HhJobPublisher>());

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
