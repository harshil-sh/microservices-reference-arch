using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Orders.Application;
using Orders.API.HealthChecks;
using Orders.Infrastructure;
using Orders.Infrastructure.Persistence;
using Orders.Infrastructure.Persistence.Seed;
using Shared.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddObservability("Orders.API", builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOrdersApplication();
builder.Services.AddOrdersInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("orders-db");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await context.Database.MigrateAsync();
        await OrdersDbContextSeed.SeedAsync(context, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the orders database");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
