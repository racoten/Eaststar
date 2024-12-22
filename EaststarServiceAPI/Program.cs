using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using EaststarServiceAPI;
using EaststarServiceAPI.HttpListeners;
using System.Net.Http;
using EaststarServiceAPI.Operators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using EaststarServiceAPI.Tasking;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwtSection = builder.Configuration.GetSection("JwtOptions");
var authority = jwtSection.GetValue<string>("Authority");
var audience = jwtSection.GetValue<string>("Audience");
var issuer = jwtSection.GetValue<string>("Issuer");
var secretKey = jwtSection.GetValue<string>("SecretKey");

builder.Services.AddAuthorization();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authority;
#pragma warning disable CS8604 // Possible null reference argument.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidAudience = audience,
            ValidIssuer = issuer,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secretKey)
            )
        };
#pragma warning restore CS8604 // Possible null reference argument.
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

StartListenerHandlingEndpoints();
StartOperatorLoginEndpoints();
StartOutputTaskingEndpoints();

app.Run();

void StartOutputTaskingEndpoints()
{
    OutputHandling outputHandling = new OutputHandling();
    TaskHandling taskHandling = new TaskHandling();

    app.MapGet("/output/get/{agentId}", async (HttpContext httpContext, string agentId) =>
    {
        try
        {
            var taskOutput = outputHandling.GetOutputFromAgent(agentId);

            if (taskOutput == null)
            {
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                await httpContext.Response.WriteAsync($"No output found for the agent with ID: {agentId}");
                return;
            }

            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsJsonAsync(taskOutput);
        }
        catch (Exception ex)
        {
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsync($"Error: {ex.Message}");
        }
    }).RequireAuthorization();

    app.MapPost("/tasks", async (HttpContext httpContext) =>
    {
        try
        {
            using var reader = new StreamReader(httpContext.Request.Body);
            var body = await reader.ReadToEndAsync();

            var tasks = JsonSerializer.Deserialize<Tasks>(body);
            if (tasks == null)
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsync("Error: Invalid request body.");
                return;
            }

            if (!taskHandling.PostTaskForAgent(tasks))
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsync($"Error: Could not post task for Agent {tasks.AgentId}");
                return;
            }

            httpContext.Response.StatusCode = StatusCodes.Status201Created;
            await httpContext.Response.WriteAsync($"Successfully added task for {tasks.AgentId}");
        }
        catch (Exception ex)
        {
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsync($"Error: {ex.Message}");
        }
    }).RequireAuthorization();

    app.MapGet("/test/getTasksForAgent", async (HttpContext httpContext, string agentId) =>
    {
        string tasks = taskHandling.GetTaskForAgent(agentId);

        if (tasks == null)
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            await httpContext.Response.WriteAsync($"No tasks found for agent with ID: {agentId}");
            return;
        } else
        {
            var response = JsonSerializer.Serialize(tasks);
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(response);
        }

    }).RequireAuthorization();
}


void StartOperatorLoginEndpoints()
{
    var operatorLoginGroup = app.MapGroup("/operator");

    operatorLoginGroup.MapPost("/login", async (HttpContext httpContext) =>
    {
        try
        {
            using var reader = new StreamReader(httpContext.Request.Body);
            var body = await reader.ReadToEndAsync();

            var operatorInfo = JsonSerializer.Deserialize<OperatorsInfo>(body);
            if (operatorInfo == null)
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsync("Error: Invalid request body.");
                return;
            }

            var login = new OperatorLogin();
            bool isAuthenticated = login.LoginOperator(operatorInfo.Username, operatorInfo.Password);

            if (!isAuthenticated)
            {
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await httpContext.Response.WriteAsync("Error: Could not authenticate user");
                return;
            }

            operatorInfo.JWToken = GenerateJwtToken(operatorInfo);

            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsJsonAsync(operatorInfo);
        }
        catch (Exception ex)
        {
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsync($"Error: {ex.Message}");
        }
    });
    operatorLoginGroup.MapPost("/register", async (HttpContext httpContext) =>
    {
        try
        {
            using var reader = new StreamReader(httpContext.Request.Body);
            var body = await reader.ReadToEndAsync();

            var operatorInfo = JsonSerializer.Deserialize<OperatorsInfo>(body);
            if (operatorInfo == null)
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsync("Error: Invalid request body.");
                return;
            }

            var register = new OperatorRegister();
            bool registered = register.RegisterOperator(operatorInfo);
            if (!registered)
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsync("Error: Could not register user.");
                return;
            }

            httpContext.Response.StatusCode = StatusCodes.Status201Created;
            await httpContext.Response.WriteAsync($"User '{operatorInfo.Username}' registered successfully.");
        }
        catch (Exception ex)
        {
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsync($"Error: {ex.Message}");
        }
    });
}
string GenerateJwtToken(OperatorsInfo operatorInfo)
{
#pragma warning disable CS8604 // Possible null reference argument.
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
#pragma warning restore CS8604 // Possible null reference argument.
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, operatorInfo.Id ?? string.Empty),
        new Claim(JwtRegisteredClaimNames.UniqueName, operatorInfo.Username ?? string.Empty),
    };

    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: claims,
        expires: DateTime.UtcNow.AddDays(30),
        signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
void StartListenerHandlingEndpoints()
{
    var listenersGroup = app.MapGroup("/listeners").RequireAuthorization();

    listenersGroup.MapPost("/create", async (HttpContext httpContext) =>
    {
        try
        {
            using (var reader = new StreamReader(httpContext.Request.Body))
            {
                var body = await reader.ReadToEndAsync();
                var listener = JsonSerializer.Deserialize<HttpListenersInfo>(body);

                if (listener != null)
                {
                    var creator = new HttpListenersCreate();
                    if (creator.CreateListener(listener))
                    {
                        httpContext.Response.StatusCode = StatusCodes.Status201Created;
                        await httpContext.Response.WriteAsync("Listener created successfully.");
                    }
                    else
                    {
                        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await httpContext.Response.WriteAsync("Failed to create listener.");
                    }
                }
                else
                {
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await httpContext.Response.WriteAsync("Invalid listener data.");
                }
            }
        }
        catch (Exception ex)
        {
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsync($"Error: {ex.Message}");
        }
    });

    listenersGroup.MapPost("/stop", async (HttpContext httpContext) =>
    {
        try
        {
            using var reader = new StreamReader(httpContext.Request.Body);
            var body = await reader.ReadToEndAsync();
            var listener = JsonSerializer.Deserialize<HttpListenersInfo>(body);

            if (listener != null)
            {
                var creator = new HttpListenersCreate();
                var success = await creator.StopListenerAsync(listener.Id);

                if (success)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status200OK;
                    await httpContext.Response.WriteAsync("Listener stopped successfully.");
                }
                else
                {
                    httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                    await httpContext.Response.WriteAsync("Could not find or stop the listener.");
                }
            }
            else
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsync("Invalid listener data.");
            }
        }
        catch (Exception ex)
        {
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsync($"Error: {ex.Message}");
        }
    });

    listenersGroup.MapGet("/", async (HttpContext httpContext) =>
    {
        var getter = new HttpListenersGet();
        var listeners = getter.GetAllListeners();
        var response = JsonSerializer.Serialize(listeners);

        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsync(response);
    });

    listenersGroup.MapGet("/{id}", async (HttpContext httpContext, string id) =>
    {
        var getter = new HttpListenersGet();
        var listener = getter.GetListenerById(id);

        if (listener != null)
        {
            var response = JsonSerializer.Serialize(listener);
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(response);
        }
        else
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            await httpContext.Response.WriteAsync("Listener not found.");
        }
    });

    listenersGroup.MapDelete("/{id}", async (HttpContext httpContext, string id) =>
    {
        var deleter = new HttpListenersDelete();
        if (deleter.DeleteListener(id))
        {
            httpContext.Response.StatusCode = StatusCodes.Status200OK;
            await httpContext.Response.WriteAsync("Listener deleted successfully.");
        }
        else
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            await httpContext.Response.WriteAsync("Listener not found.");
        }
    });
}