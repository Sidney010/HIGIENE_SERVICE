using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Agenda.Api.Middleware;
using Agenda.Api.Security;
using Agenda.Application;
using Agenda.Application.Abstractions;
using Agenda.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------- camadas
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ITenantProvider, HttpContextTenantProvider>();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

// ---------------------------------------------------------------- autenticação / autorização (JWT)
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Secao));
builder.Services.AddSingleton<JwtTokenService>();

var jwt = builder.Configuration.GetSection(JwtOptions.Secao).Get<JwtOptions>()
          ?? throw new InvalidOperationException("Seção 'Jwt' ausente no appsettings.");
if (jwt.Key.Length < 32) throw new InvalidOperationException("Jwt:Key deve ter ao menos 32 caracteres.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false; // mantém os nomes das claims (sub, perfil, estabelecimento_id)
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = jwt.Issuer,
            ValidateAudience = true, ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            RoleClaimType = "perfil", // [Authorize(Roles = "Admin,...")] lê a claim "perfil"
        };
    });
builder.Services.AddAuthorization();

// ---------------------------------------------------------------- MVC (controllers) + JSON + erros de binding
builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(o =>
    {
        o.InvalidModelStateResponseFactory = ctx =>
        {
            var erros = ctx.ModelState
                .Where(kv => kv.Value is { Errors.Count: > 0 })
                .ToDictionary(kv => kv.Key, kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

            var problema = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Requisição inválida",
                Type = "https://api.exemplo.com/erros/VAL_INVALIDO",
                Instance = ctx.HttpContext.Request.Path,
            };
            problema.Extensions["codigo"] = "VAL_INVALIDO";
            problema.Extensions["erros"] = erros;
            problema.Extensions["correlationId"] = ctx.HttpContext.Items[CorrelationIdMiddleware.Header];
            return new BadRequestObjectResult(problema) { ContentTypes = { "application/problem+json" } };
        };
    });

// ---------------------------------------------------------------- rate limiting (login / OTP)
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

// ---------------------------------------------------------------- health + OpenAPI
builder.Services.AddHealthChecks().AddCheck<DbHealthCheck>("db", tags: ["ready"]);
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsProduction()) app.UseHttpsRedirection();

app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

if (app.Environment.IsDevelopment()) app.MapOpenApi(); // /openapi/v1.json (gerado do código; o contrato oficial é docs/openapi.yaml)

app.MapControllers();

static Task EscreverStatus(HttpContext ctx, HealthReport relatorio)
{
    ctx.Response.ContentType = "application/json";
    return ctx.Response.WriteAsJsonAsync(new { status = relatorio.Status.ToString() });
}

app.MapHealthChecks("/api/v1/health", new HealthCheckOptions { Predicate = _ => false, ResponseWriter = EscreverStatus })
   .AllowAnonymous();
app.MapHealthChecks("/api/v1/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready"), ResponseWriter = EscreverStatus })
   .AllowAnonymous();

app.Run();

// Necessário para WebApplicationFactory<Program> nos testes de API.
public partial class Program { }
