using ApiGateway.Config;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Host.UseSerilog((_, config) => config.ReadFrom.Configuration(builder.Configuration));

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

builder.Services.AddCors(options =>
{
    string _origins = "http://localhost:5173";
                      
    string[] origins = _origins.Split(",");
    options.AddDefaultPolicy((policyBuilder =>
    {
        policyBuilder.WithOrigins(origins)
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .AllowCredentials()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("*");   
    }));
});

builder.Services.AddReverseProxy(builder.Configuration);

builder.Services.AddOAuthProxy();

builder.Services.AddAuthorizationPolicies();

builder.Services.AddCache(builder.Configuration);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapReverseProxy();

app.Run();