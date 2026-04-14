using Boilerate.Application;
using Boilerate.Host.Configurations;
using Boilerate.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddConfigurations();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Boilerate API",
        Version = "v1"
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseInfrastructure(builder.Configuration);
app.MapEndpoints();

await app.RunAsync();
