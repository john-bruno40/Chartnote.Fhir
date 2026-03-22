using Chartnote.Fhir.Api.Providers;
using Chartnote.Fhir.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Register the provider factory
builder.Services.AddSingleton<EhrProviderFactory>();

// Register FhirService
builder.Services.AddScoped<FhirService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevHarness", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();
app.UseCors("DevHarness");
app.Run();