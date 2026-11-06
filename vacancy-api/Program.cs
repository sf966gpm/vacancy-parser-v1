using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Vacancy.Api.Features.HhJobs;
using Vacancy.Api.Features.HhPresets;
using Vacancy.Api.Features.Vacancies;
using Vacancy.Api.Persistence;
using Vacancy.Api.Messaging.RabbitMq;
using Vacancy.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("VacancyDatabase")));

builder.Services.AddScoped<IHhJobService, HhJobService>();
builder.Services.AddScoped<IHhSearchPresetService, HhSearchPresetService>();
builder.Services.AddScoped<IHhVacancyIngestionService, HhVacancyIngestionService>();
builder.Services.AddSingleton<HhJobPublisher>();
builder.Services.AddSingleton<IHhJobPublisher>(services =>
    services.GetRequiredService<HhJobPublisher>());
builder.Services.AddHostedService(services =>
    services.GetRequiredService<HhJobPublisher>());
builder.Services.AddHostedService<HhJobOutboxDispatcher>();
builder.Services.AddHostedService<HhVacancyBatchConsumer>();

var app = builder.Build();

if (args.Contains("--migrate", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    return;
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

await app.RunAsync();
