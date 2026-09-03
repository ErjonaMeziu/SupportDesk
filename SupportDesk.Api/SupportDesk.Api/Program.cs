using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Common.Middleware;
using SupportDesk.Api.Data;
using SupportDesk.Api.Services;

var builder = WebApplication.CreateBuilder(args);

const string AngularDevCorsPolicy = "AngularDev";

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enums as readable strings ("New", "InProgress", ...) on the wire instead of numbers.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<SupportDeskDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SupportDesk")));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ITicketReferenceGenerator, SqlTicketReferenceGenerator>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IAgentService, AgentService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularDevCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:62665")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SupportDeskDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors(AngularDevCorsPolicy);

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
