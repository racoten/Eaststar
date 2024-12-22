using EaststarServiceAPI.Agents;
using EaststarServiceAPI.Operators;
using EaststarServiceAPI.Tasking;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text;

namespace EaststarServiceAPI.HttpListeners
{
    public class HttpListenersCreateForAgents
    {
        private static bool _initialized;
        private static string? _globalAuthority;
        private static string? _globalIssuer;
        private static string? _globalAudience;
        private static string? _globalSecretKey;

        public HttpListenersCreateForAgents()
        {
            if (_initialized) return;
            _globalAuthority = $"https://{GenerateRandomString(8)}.local";
            _globalIssuer = $"https://{GenerateRandomString(8)}-issuer.local";
            _globalAudience = GenerateRandomString(12);
            _globalSecretKey = GenerateRandomString(32);
            _initialized = true;
        }
        public static async Task StartListener(HttpListenersInfo? listenerInfo, CancellationToken token)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseKestrel().UseUrls($"http://{listenerInfo.Host}:{listenerInfo.Port}/");

            builder.Services.AddAuthorization();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = _globalAuthority;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidAudience = _globalAudience,
                        ValidIssuer = _globalIssuer,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_globalSecretKey))
                    };
                });

            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();

            var agentsGroup = app.MapGroup("/agents");
            TaskHandling taskHandling = new TaskHandling();
            OutputHandling outputHandling = new OutputHandling();

            agentsGroup.MapPost("/register", static async httpContext =>
            {
                try
                {
                    using var reader = new StreamReader(httpContext.Request.Body);
                    var body = await reader.ReadToEndAsync();
                    var agentsInfo = JsonSerializer.Deserialize<AgentsInfo>(body);
                    if (agentsInfo == null)
                    {
                        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await httpContext.Response.WriteAsync("Error: Invalid request body.");
                        return;
                    }

                    agentsInfo.JWToken = GenerateJwtToken(agentsInfo);
                    agentsInfo.Id = new Guid().ToString();
                    var register = new AgentRegister();

                    bool registered = register.Register(agentsInfo);
                    if (!registered)
                    {
                        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await httpContext.Response.WriteAsync("Error: Could not register agent.");
                        return;
                    }

                    httpContext.Response.StatusCode = StatusCodes.Status201Created;
                    httpContext.Response.ContentType = "application/json";
                    await httpContext.Response.WriteAsJsonAsync(agentsInfo);
                }
                catch (Exception ex)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await httpContext.Response.WriteAsync($"Error: {ex.Message}");
                }
            });

            agentsGroup.MapPost("/healthCheck", async (HttpContext httpContext) =>
            {
                try
                {
                    using var reader = new StreamReader(httpContext.Request.Body);
                    var body = await reader.ReadToEndAsync();
                    var agentsInfo = JsonSerializer.Deserialize<AgentsInfo>(body);
                    if (agentsInfo == null)
                    {
                        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await httpContext.Response.WriteAsync("Error: Invalid request body.");
                        return;
                    }

                    AgentHealthCheck agentHealthCheck = new AgentHealthCheck();
                    if (!agentHealthCheck.HealthCheck(agentsInfo))
                    {
                        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await httpContext.Response.WriteAsync($"Error: Could not update health ping for agent {agentsInfo.Id}.");
                        return;
                    }

                    httpContext.Response.StatusCode = StatusCodes.Status200OK;
                }
                catch (Exception ex)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await httpContext.Response.WriteAsync($"Error: {ex.Message}");
                }
            }).RequireAuthorization();

            agentsGroup.MapGet("/{agentId}/getTasks", async (HttpContext httpContext, string agentId) =>
            {
                try
                {
                    var tasks = taskHandling.GetTaskForAgent(agentId);

                    if (!tasks.Any())
                    {
                        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                        await httpContext.Response.WriteAsync("No tasks found for the specified agent.");
                        return;
                    }

                    httpContext.Response.StatusCode = StatusCodes.Status200OK;
                    httpContext.Response.ContentType = "application/json";
                    await httpContext.Response.WriteAsJsonAsync(tasks);
                }
                catch (Exception ex)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await httpContext.Response.WriteAsync($"Error: {ex.Message}");
                }
            }).RequireAuthorization();

            agentsGroup.MapPost("/{agentId}/postTaskOutput", async (HttpContext httpContext, string agentId) =>
            {
                try
                {
                    var outputJson = await JsonSerializer.DeserializeAsync<TaskOutput>(httpContext.Request.Body);
                    if (outputJson == null || string.IsNullOrEmpty(outputJson.Output))
                    {
                        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await httpContext.Response.WriteAsync("Error: Invalid or missing task output.");
                        return;
                    }

                    var taskOutput = outputHandling.PostOutputForOperator(agentId, outputJson.Output);

                    httpContext.Response.StatusCode = StatusCodes.Status201Created;
                    httpContext.Response.ContentType = "application/json";
                    await httpContext.Response.WriteAsJsonAsync(taskOutput);
                }
                catch (JsonException)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await httpContext.Response.WriteAsync("Error: Invalid JSON format.");
                }
                catch (Exception ex)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await httpContext.Response.WriteAsync($"Error: {ex.Message}");
                }
            }).RequireAuthorization();


            app.MapGet("/hello", () => "Hello From Endpoint with Authorization").RequireAuthorization();
            await app.RunAsync(token);
        }

        private static string GenerateJwtToken(AgentsInfo agentInfo)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_globalSecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim("agent_id", agentInfo.Id ?? string.Empty),
                new Claim("agent_name", agentInfo.Name ?? string.Empty),
            };

            var token = new JwtSecurityToken(
                issuer: _globalIssuer,
                audience: _globalAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(30),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Range(0, length).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        }
    }
}
