using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EaststarServiceAPI.Agents;
using EaststarServiceAPI.Operators;
using EaststarServiceAPI.Tasking;

namespace EaststarServiceAPI.HttpListeners
{
    public class HttpListenersCreate
    {
        private static bool _initialized;
        private static string? _globalAuthority;
        private static string? _globalIssuer;
        private static string? _globalAudience;
        private static string? _globalSecretKey;

        private static readonly Dictionary<string, CancellationTokenSource> _ctsMap = new();
        private static readonly Dictionary<string, Task> _serverTasks = new();

        public HttpListenersCreate()
        {
            if (_initialized) return;
            _globalAuthority = $"https://{GenerateRandomString(8)}.local";
            _globalIssuer = $"https://{GenerateRandomString(8)}-issuer.local";
            _globalAudience = GenerateRandomString(12);
            _globalSecretKey = GenerateRandomString(32);
            _initialized = true;
        }

        public bool CreateListener(HttpListenersInfo listener)
        {
            try
            {
                using var context = new EaststarContext();
                context.HttpListenersInfo.Add(listener);
                context.SaveChanges();

                var cts = new CancellationTokenSource();
                _ctsMap[listener.Id] = cts;
                _serverTasks[listener.Id] = Task.Run(() => StartListener(listener, cts.Token));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating listener: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> StopListenerAsync(string listenerId)
        {
            if (!_ctsMap.ContainsKey(listenerId)) return false;

            var cts = _ctsMap[listenerId];
            var task = _serverTasks[listenerId];

            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
                try
                {
                    await task;
                }
                catch (OperationCanceledException)
                {
                }
            }

            _ctsMap.Remove(listenerId);
            _serverTasks.Remove(listenerId);

            try
            {
                return RemoveListener(listenerId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return true;
        }

        private bool RemoveListener(string id)
        {
            var deleter = new HttpListenersDelete();
            return deleter.DeleteListener(id);
        }

        private async Task StartListener(HttpListenersInfo? listenerInfo, CancellationToken token)
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

            app.MapPost("/agentRegister", async httpContext =>
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
                    var register = new AgentRegister();
                    bool registered = register.Register(agentsInfo);
                    if (!registered)
                    {
                        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await httpContext.Response.WriteAsync("Error: Could not register user.");
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



            app.MapGet("/hello", () => "Hello From Endpoint with Authorization").RequireAuthorization();
            await app.RunAsync(token);
        }

        private string GenerateJwtToken(AgentsInfo agentInfo)
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

        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Range(0, length).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        }
    }
}
