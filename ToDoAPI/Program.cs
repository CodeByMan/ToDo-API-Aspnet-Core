using Newtonsoft.Json;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Converters;
using ToDoAPI_ASPNET.Data;
using ToDoAPI_ASPNET.Models.Config;
using ToDoAPI_ASPNET.Services.Auth;
using ToDoAPI_ASPNET.Services.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using ToDoAPI_ASPNET.Repositories.Auth;
using ToDoAPI_ASPNET.Repositories.Users;
using ToDoAPI_ASPNET.Services.ToDoItems;
using ToDoAPI_ASPNET.Repositories.ToDoItems;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ToDoAPI_ASPNET.Services.Utils;

// Prevent Microsoft Identity from remapping standard JWT claim names.
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        options.SerializerSettings.Converters.Add(new StringEnumConverter());
        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
    });

#region AutoMapper Configuration
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
#endregion

builder.Services.AddEndpointsApiExplorer();

ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

#region Optional Database Migration
if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<DataContext>();
    await context.Database.MigrateAsync();
}
#endregion

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("ConfiguredOrigins");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static void ConfigureServices(
    IServiceCollection services,
    IConfiguration configuration)
{
    #region API Versioning
    services.AddApiVersioning(options =>
    {
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.DefaultApiVersion = new ApiVersion(1, 0);
    });
    #endregion

    #region CORS Configuration
    var allowedOrigins = configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? Array.Empty<string>();

    services.AddCors(options =>
    {
        options.AddPolicy("ConfiguredOrigins", policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            }
        });
    });
    #endregion

    #region SQL Server Configuration
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection must be provided through configuration or environment variables.");
    }

    services.AddDbContext<DataContext>(options =>
    {
        options.UseSqlServer(connectionString);
    });
    #endregion

    #region Authentication Configuration
    var jwt = configuration.GetSection("JWT").Get<JWTSettings>()
        ?? throw new InvalidOperationException("JWT configuration is missing.");

    ValidateJwtSettings(jwt);

    var signingKey = new SymmetricSecurityKey(Base64UrlEncoder.DecodeBytes(jwt.Key));

    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var tokenType = context.Principal?
                    .FindFirst(TokenUtil.TokenTypeClaim)?
                    .Value;

                if (!string.Equals(tokenType, "access", StringComparison.Ordinal))
                {
                    context.Fail("Only access tokens may authenticate protected API endpoints.");
                }

                return Task.CompletedTask;
            }
        };
    });
    #endregion

    #region Swagger Documentation
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "ToDo API",
            Version = "1.0",
            Description = "A user-owned task management REST API built with ASP.NET Core and SQL Server.",
            Contact = new OpenApiContact
            {
                Name = "Muhammad Ali Nawaz"
            }
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "Enter a JWT access token using the format: Bearer {token}",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
    #endregion

    #region Validation Configuration
    services.Configure<ApiBehaviorOptions>(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });
    #endregion

    #region JWT Data Binding
    services.Configure<JWTSettings>(configuration.GetSection("JWT"));
    services.AddSingleton(jwt);
    #endregion

    #region Logging Configuration
    services.AddLogging();
    #endregion

    #region Repository Configuration
    services.AddScoped<IAuthRepository, AuthRepository>();
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IToDoItemRepository, ToDoItemRepository>();
    #endregion

    #region Services Configuration
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<IUserService, UserService>();
    services.AddScoped<IToDoItemService, ToDoItemService>();
    #endregion
}

static void ValidateJwtSettings(JWTSettings jwt)
{
    if (string.IsNullOrWhiteSpace(jwt.Key))
        throw new InvalidOperationException("JWT:Key must be configured.");

    byte[] keyBytes;
    try
    {
        keyBytes = Base64UrlEncoder.DecodeBytes(jwt.Key);
    }
    catch (FormatException ex)
    {
        throw new InvalidOperationException("JWT:Key must be a valid Base64Url-encoded value.", ex);
    }

    if (keyBytes.Length < 32)
        throw new InvalidOperationException("JWT:Key must decode to at least 32 bytes.");

    if (string.IsNullOrWhiteSpace(jwt.Issuer))
        throw new InvalidOperationException("JWT:Issuer must be configured.");

    if (string.IsNullOrWhiteSpace(jwt.Audience))
        throw new InvalidOperationException("JWT:Audience must be configured.");

    if (jwt.AccessTokenExpiry <= 0)
        throw new InvalidOperationException("JWT:AccessTokenExpiry must be greater than zero hours.");

    if (jwt.RefreshTokenExpiry <= 0)
        throw new InvalidOperationException("JWT:RefreshTokenExpiry must be greater than zero days.");
}

public partial class Program
{
}
