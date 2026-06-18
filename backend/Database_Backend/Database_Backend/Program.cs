using Database_Backend.Models;
using Database_Backend.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<DatabaseProjectContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtIssuer = jwtSection["Issuer"] ?? "DatabaseBackend";
var jwtAudience = jwtSection["Audience"] ?? "DatabaseBackendClient";
var jwtKey = jwtSection["Key"] ?? "change-this-super-secret-key-for-production-min-32-chars";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddProblemDetails();

builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var details = new ValidationProblemDetails(context.ModelState)
        {
            Type = "https://httpstatuses.com/400",
            Title = "Request validation failed",
            Status = StatusCodes.Status400BadRequest,
            Detail = "One or more validation errors occurred.",
            Instance = context.HttpContext.Request.Path
        };

        details.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        details.Extensions["timestamp"] = DateTimeOffset.UtcNow;

        return new BadRequestObjectResult(details)
        {
            ContentTypes = { "application/problem+json" }
        };
    };
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}'"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var logger = context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("GlobalExceptionHandler");

        var exceptionFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        if (exceptionFeature?.Error is not null)
        {
            logger.LogError(exceptionFeature.Error, "Unhandled exception for {Path}", context.Request.Path);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var details = new ProblemDetails
        {
            Type = "https://httpstatuses.com/500",
            Title = "Unexpected server error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "An unexpected error occurred while processing the request.",
            Instance = context.Request.Path
        };

        details.Extensions["traceId"] = context.TraceIdentifier;
        details.Extensions["timestamp"] = DateTimeOffset.UtcNow;

        var payload = JsonSerializer.Serialize(details);
        await context.Response.WriteAsync(payload);
    });
});

app.UseStatusCodePages(async statusCodeContext =>
{
    var context = statusCodeContext.HttpContext;
    var response = context.Response;

    if (response.StatusCode < 400 || response.HasStarted)
    {
        return;
    }

    if (response.ContentLength.HasValue && response.ContentLength.Value > 0)
    {
        return;
    }

    var title = ReasonPhrases.GetReasonPhrase(response.StatusCode);
    var details = new ProblemDetails
    {
        Type = $"https://httpstatuses.com/{response.StatusCode}",
        Title = string.IsNullOrWhiteSpace(title) ? "Request failed" : title,
        Status = response.StatusCode,
        Detail = "The request could not be processed.",
        Instance = context.Request.Path
    };

    details.Extensions["traceId"] = context.TraceIdentifier;
    details.Extensions["timestamp"] = DateTimeOffset.UtcNow;

    response.ContentType = "application/problem+json";
    var payload = JsonSerializer.Serialize(details);
    await response.WriteAsync(payload);
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
