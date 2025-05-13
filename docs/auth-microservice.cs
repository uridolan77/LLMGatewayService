using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using System.Reflection;

namespace IdentityService
{
    #region Program and Startup

    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                Log.Information("Starting Identity Microservice");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext())
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }

    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Configuration
            services.Configure<JwtSettings>(Configuration.GetSection("JwtSettings"));
            services.Configure<PasswordPolicySettings>(Configuration.GetSection("PasswordPolicy"));
            services.Configure<UserOptions>(Configuration.GetSection("UserOptions"));
            
            // Database
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

            // MediatR for CQRS pattern
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Startup).Assembly));

            // Identity with custom options
            services.AddIdentityCore<ApplicationUser>(options =>
                {
                    // Configure identity options based on settings
                    var passwordPolicy = Configuration.GetSection("PasswordPolicy").Get<PasswordPolicySettings>();
                    options.Password.RequiredLength = passwordPolicy.MinimumLength;
                    options.Password.RequireDigit = passwordPolicy.RequireDigit;
                    options.Password.RequireLowercase = passwordPolicy.RequireLowercase;
                    options.Password.RequireUppercase = passwordPolicy.RequireUppercase;
                    options.Password.RequireNonAlphanumeric = passwordPolicy.RequireSpecialCharacter;
                    
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(passwordPolicy.LockoutDurationMinutes);
                    options.Lockout.MaxFailedAccessAttempts = passwordPolicy.MaxFailedAttempts;
                    options.Lockout.AllowedForNewUsers = true;
                    
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // JWT Authentication
            var jwtSettings = Configuration.GetSection("JwtSettings").Get<JwtSettings>();
            var key = Encoding.ASCII.GetBytes(jwtSettings.Secret);
            
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = !Environment.IsDevelopment();
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ClockSkew = TimeSpan.Zero // Reduce the default 5 min clock skew for tighter token lifetimes
                    };

                    // Capture JWT token validation events for logging
                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            Log.Warning("Authentication failed: {Message}", context.Exception.Message);
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
                            var userId = context.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
                            var user = userService.GetUserById(userId).Result;
                            
                            // Check if user exists and is active
                            if (user == null || !user.IsActive)
                            {
                                context.Fail("Unauthorized user");
                            }
                            
                            return Task.CompletedTask;
                        }
                    };
                });

            // Authorization with policies
            services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
                options.AddPolicy("RequirePartnerRole", policy => policy.RequireRole("Partner"));
                options.AddPolicy("RequireSubpartnerRole", policy => policy.RequireRole("Subpartner"));
                
                // Custom policy for additional authorization requirements
                options.AddPolicy("WhitelabelAccess", policy =>
                    policy.Requirements.Add(new WhitelabelAccessRequirement()));
            });
            
            // Register custom authorization handlers
            services.AddSingleton<IAuthorizationHandler, WhitelabelAccessHandler>();

            // Register Services
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<ITwoFactorService, TwoFactorService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IPasswordService, PasswordService>();
            
            // Redis for distributed caching (token blacklisting, etc.)
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = Configuration.GetConnectionString("RedisConnection");
                options.InstanceName = "IdentityService";
            });

            // Add validators
            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // API Controllers
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            // API Documentation with Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Identity Microservice API",
                    Version = "v1",
                    Description = "A comprehensive API for user management and authentication"
                });
                
                // Add JWT Authentication to Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
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
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header
                        },
                        new List<string>()
                    }
                });
            });

            // Health Checks
            services.AddHealthChecks()
                .AddDbContextCheck<ApplicationDbContext>()
                .AddRedis(Configuration.GetConnectionString("RedisConnection"), name: "redis");
                
            // CORS Policy
            services.AddCors(options =>
            {
                options.AddPolicy("DefaultCorsPolicy", builder =>
                {
                    builder.WithOrigins(Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });
            
            // Rate limiting
            services.AddRateLimiting(Configuration);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Development specific configuration
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity Microservice API v1"));
            }
            else
            {
                // Production specific middleware
                app.UseExceptionHandler("/error");
                app.UseHsts();
            }

            // Security Headers Middleware
            app.UseSecurityHeaders();
            
            // Configure exception handling
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            
            // Configure request logging
            app.UseSerilogRequestLogging();
            
            // Standard middleware
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors("DefaultCorsPolicy");
            
            // Rate limiting middleware
            app.UseRateLimiting();
            
            // Auth middleware
            app.UseAuthentication();
            app.UseAuthorization();

            // Add middleware for IP tracking and other security features
            app.UseMiddleware<IPTrackingMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health").AllowAnonymous();
            });
        }
    }
    
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure rate limiting based on configuration
            // Implementation depends on which rate limiting library you choose
            // Example using AspNetCoreRateLimit
            /*
            services.AddMemoryCache();
            services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
            services.Configure<IpRateLimitPolicies>(configuration.GetSection("IpRateLimitPolicies"));
            services.AddInMemoryRateLimiting();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            */
            
            return services;
        }
    }
    
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
        {
            // Apply rate limiting middleware
            // Example: app.UseIpRateLimiting();
            return app;
        }
        
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            // Apply security headers
            app.Use(async (context, next) =>
            {
                // Add security headers
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Add("X-Frame-Options", "DENY");
                context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
                context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
                context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'; frame-ancestors 'none'");
                context.Response.Headers.Add("Permissions-Policy", "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");
                
                await next();
            });
            
            return app;
        }
    }

    #endregion

    #region Sample Configuration

    /*
    Example appsettings.json configuration:
    
    {
      "ConnectionStrings": {
        "DefaultConnection": "Server=your-sql-server;Database=IdentityDb;User Id=user;Password=password;MultipleActiveResultSets=true",
        "RedisConnection": "your-redis-server:6379,password=password"
      },
      "JwtSettings": {
        "Secret": "your-super-secret-key-with-at-least-32-characters",
        "Issuer": "identity-service",
        "Audience": "api-clients",
        "AccessTokenExpirationMinutes": 15,
        "RefreshTokenExpirationDays": 7
      },
      "PasswordPolicy": {
        "MinimumLength": 8,
        "RequireDigit": true,
        "RequireLowercase": true,
        "RequireUppercase": true,
        "RequireSpecialCharacter": true,
        "PasswordExpirationDays": 90,
        "PasswordHistoryCount": 5,
        "MaxFailedAttempts": 5,
        "LockoutDurationMinutes": 15
      },
      "UserOptions": {
        "RequireEmailConfirmation": true,
        "RequirePhoneConfirmation": false,
        "EnableTwoFactorByDefault": false,
        "CustomUserFields": ["Company", "Department", "EmployeeId"]
      },
      "AllowedOrigins": [
        "https://yourapp.com",
        "https://admin.yourapp.com"
      ],
      "IpRateLimiting": {
        "EnableEndpointRateLimiting": true,
        "StackBlockedRequests": false,
        "RealIpHeader": "X-Real-IP",
        "ClientIdHeader": "X-ClientId",
        "HttpStatusCode": 429,
        "GeneralRules": [
          {
            "Endpoint": "*:/api/v1/auth/login",
            "Period": "1m",
            "Limit": 5
          },
          {
            "Endpoint": "*:/api/v1/auth/forgot-password",
            "Period": "15m",
            "Limit": 2
          },
          {
            "Endpoint": "*",
            "Period": "1s",
            "Limit": 10
          },
          {
            "Endpoint": "*",
            "Period": "15m",
            "Limit": 100
          }
        ]
      },
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft": "Warning",
          "Microsoft.Hosting.Lifetime": "Information"
        }
      },
      "Serilog": {
        "MinimumLevel": {
          "Default": "Information",
          "Override": {
            "Microsoft": "Warning",
            "System": "Warning"
          }
        },
        "WriteTo": [
          {
            "Name": "Console"
          },
          {
            "Name": "File",
            "Args": {
              "path": "logs/identity-service-.log",
              "rollingInterval": "Day",
              "retainedFileCountLimit": 7
            }
          }
        ],
        "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
      },
      "HealthChecksUI": {
        "HealthChecks": [
          {
            "Name": "Identity Service",
            "Uri": "/health"
          }
        ],
        "EvaluationTimeInSeconds": 10,
        "MinimumSecondsBetweenFailureNotifications": 60
      }
    }
    */

    #endregion

    #region Docker Config

    /*
    Example Dockerfile:

    FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
    WORKDIR /app
    EXPOSE 80
    EXPOSE 443

    FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
    WORKDIR /src
    COPY ["IdentityService/IdentityService.csproj", "IdentityService/"]
    RUN dotnet restore "IdentityService/IdentityService.csproj"
    COPY . .
    WORKDIR "/src/IdentityService"
    RUN dotnet build "IdentityService.csproj" -c Release -o /app/build

    FROM build AS publish
    RUN dotnet publish "IdentityService.csproj" -c Release -o /app/publish

    FROM base AS final
    WORKDIR /app
    COPY --from=publish /app/publish .
    ENTRYPOINT ["dotnet", "IdentityService.dll"]

    Example docker-compose.yml:

    version: '3.8'

    services:
      identity-service:
        build:
          context: .
          dockerfile: IdentityService/Dockerfile
        ports:
          - "5001:80"
          - "5002:443"
        environment:
          - ASPNETCORE_ENVIRONMENT=Development
          - ConnectionStrings__DefaultConnection=Server=sql-server;Database=IdentityDb;User Id=sa;Password=YourStrong!Password;MultipleActiveResultSets=true
          - ConnectionStrings__RedisConnection=redis:6379
        depends_on:
          - sql-server
          - redis
        networks:
          - microservice-network

      sql-server:
        image: mcr.microsoft.com/mssql/server:2019-latest
        environment:
          - ACCEPT_EULA=Y
          - SA_PASSWORD=YourStrong!Password
        ports:
          - "1433:1433"
        volumes:
          - sql-data:/var/opt/mssql
        networks:
          - microservice-network

      redis:
        image: redis:alpine
        ports:
          - "6379:6379"
        networks:
          - microservice-network

    networks:
      microservice-network:
        driver: bridge

    volumes:
      sql-data:
    */

    #endregion
}

    #region Validators

    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Username).NotEmpty().WithMessage("Username is required");
            RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required");
        }
    }

    public class RegisterUserRequestValidator : AbstractValidator<RegisterUserRequest>
    {
        private readonly ApplicationDbContext _context;
        private readonly IOptions<PasswordPolicySettings> _passwordPolicyOptions;

        public RegisterUserRequestValidator(
            ApplicationDbContext context,
            IOptions<PasswordPolicySettings> passwordPolicyOptions)
        {
            _context = context;
            _passwordPolicyOptions = passwordPolicyOptions;

            var policy = _passwordPolicyOptions.Value;

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters")
                .MaximumLength(50).WithMessage("Username must not exceed 50 characters")
                .Matches("^[a-zA-Z0-9_.-]+$").WithMessage("Username can only contain letters, numbers, dots, underscores, and hyphens")
                .MustAsync(BeUniqueUsername).WithMessage("Username is already taken");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Email is not valid")
                .MustAsync(BeUniqueEmail).WithMessage("Email is already registered");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(policy.MinimumLength).WithMessage($"Password must be at least {policy.MinimumLength} characters");

            if (policy.RequireDigit)
                RuleFor(x => x.Password).Matches("[0-9]").WithMessage("Password must contain at least one digit");

            if (policy.RequireLowercase)
                RuleFor(x => x.Password).Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter");

            if (policy.RequireUppercase)
                RuleFor(x => x.Password).Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter");

            if (policy.RequireSpecialCharacter)
                RuleFor(x => x.Password).Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("Passwords do not match");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(50).WithMessage("First name must not exceed 50 characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(50).WithMessage("Last name must not exceed 50 characters");
        }

        private async Task<bool> BeUniqueUsername(string username, CancellationToken cancellationToken)
        {
            return !await _context.Users.AnyAsync(u => u.UserName.ToLower() == username.ToLower(), cancellationToken);
        }

        private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
        {
            return !await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
        }
    }

    public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
    {
        public UpdateUserRequestValidator()
        {
            RuleFor(x => x.FirstName)
                .MaximumLength(50).WithMessage("First name must not exceed 50 characters");

            RuleFor(x => x.LastName)
                .MaximumLength(50).WithMessage("Last name must not exceed 50 characters");

            RuleFor(x => x.PhoneNumber)
                .Matches("^[0-9+()-]*$").WithMessage("Phone number can only contain numbers, +, (), and -");
        }
    }

    public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
    {
        private readonly IOptions<PasswordPolicySettings> _passwordPolicyOptions;

        public ChangePasswordRequestValidator(IOptions<PasswordPolicySettings> passwordPolicyOptions)
        {
            _passwordPolicyOptions = passwordPolicyOptions;
            var policy = _passwordPolicyOptions.Value;

            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("Current password is required");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New password is required")
                .MinimumLength(policy.MinimumLength).WithMessage($"Password must be at least {policy.MinimumLength} characters")
                .NotEqual(x => x.CurrentPassword).WithMessage("New password cannot be the same as the current password");

            if (policy.RequireDigit)
                RuleFor(x => x.NewPassword).Matches("[0-9]").WithMessage("Password must contain at least one digit");

            if (policy.RequireLowercase)
                RuleFor(x => x.NewPassword).Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter");

            if (policy.RequireUppercase)
                RuleFor(x => x.NewPassword).Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter");

            if (policy.RequireSpecialCharacter)
                RuleFor(x => x.NewPassword).Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
        }
    }

    public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
    {
        private readonly ApplicationDbContext _context;

        public CreateRoleRequestValidator(ApplicationDbContext context)
        {
            _context = context;

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Role name is required")
                .MaximumLength(50).WithMessage("Role name must not exceed 50 characters")
                .MustAsync(BeUniqueRoleName).WithMessage("Role name already exists");

            RuleFor(x => x.Description)
                .MaximumLength(200).WithMessage("Description must not exceed 200 characters");
        }

        private async Task<bool> BeUniqueRoleName(string name, CancellationToken cancellationToken)
        {
            return !await _context.Roles.AnyAsync(r => r.Name.ToLower() == name.ToLower(), cancellationToken);
        }
    }

    #endregion

    #region CQRS Commands and Handlers

    // Example CQRS command and handler for login
    public class LoginCommand : IRequest<LoginResponse>
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
        public string TwoFactorCode { get; set; }
        public string TwoFactorRecoveryCode { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
    }

    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
    {
        private readonly IAuthService _authService;

        public LoginCommandHandler(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var loginRequest = new LoginRequest
            {
                Username = request.Username,
                Password = request.Password,
                RememberMe = request.RememberMe,
                TwoFactorCode = request.TwoFactorCode,
                TwoFactorRecoveryCode = request.TwoFactorRecoveryCode
            };

            return await _authService.Login(loginRequest, request.IpAddress, request.UserAgent);
        }
    }

    // Example CQRS query and handler for getting a user
    public class GetUserByIdQuery : IRequest<UserResponse>
    {
        public string Id { get; set; }
    }

    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserResponse>
    {
        private readonly IUserService _userService;

        public GetUserByIdQueryHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<UserResponse> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            return await _userService.GetUserById(request.Id);
        }
    }

    #endregion

    #region Configuration

    public static class ConfigureSwaggerOptions
    {
        public static void AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo 
                { 
                    Title = "Identity Microservice API", 
                    Version = "v1",
                    Description = "A comprehensive API for user management and authentication",
                    Contact = new OpenApiContact
                    {
                        Name = "API Support",
                        Email = "support@example.com",
                        Url = new Uri("https://example.com/support")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "License",
                        Url = new Uri("https://example.com/license")
                    }
                });
                
                // Add JWT Authentication
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
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
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header
                        },
                        new List<string>()
                    }
                });

                // Include XML comments
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });
        }
    }

    public static class ConfigureJwtOptions
    {
        public static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();
            var key = Encoding.ASCII.GetBytes(jwtSettings.Secret);
            
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // Set to true in production
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero // Reduce the default 5 min clock skew for tighter token lifetimes
                };

                // Capture JWT token validation events for logging
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        }
    }

    #endregion

    #region Security Extensions

    public static class SecurityExtensions
    {
        // Add additional security headers to responses
        public static void UseSecurityHeaders(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                // Add security headers
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Add("X-Frame-Options", "DENY");
                context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
                context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
                context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'; frame-ancestors 'none'");
                context.Response.Headers.Add("Permissions-Policy", "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");
                
                await next();
            });
        }

        // Enhance password security
        public static string HashPassword(string password, byte[] salt = null)
        {
            if (salt == null)
            {
                // Generate a random salt
                salt = new byte[16];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(salt);
                }
            }

            // Derive a 256-bit subkey (use HMACSHA256 with 100,000 iterations)
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            // Format: iterations.salt.hash
            return $"100000.{Convert.ToBase64String(salt)}.{hashed}";
        }

        public static bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            var parts = hashedPassword.Split('.');
            if (parts.Length != 3)
            {
                return false;
            }

            var iterations = int.Parse(parts[0]);
            var salt = Convert.FromBase64String(parts[1]);
            var hash = parts[2];

            var hashOfProvided = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: providedPassword,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: iterations,
                numBytesRequested: 256 / 8));

            return hash == hashOfProvided;
        }

        // Generate secure random tokens
        public static string GenerateSecureToken(int length = 32)
        {
            var bytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes);
        }
    }

    #endregion

    #region Domain Models

    public class ApplicationUser
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserName { get; set; }
        public string Email { get; set; }
        public string NormalizedEmail { get; set; }
        public string PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool EmailConfirmed { get; set; }
        public string PasswordHash { get; set; }
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
        public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();
        public bool TwoFactorEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; } = true;
        public string FirstName { get; set; }
        public string LastName { get; set; }
        
        // Custom fields for ProgressPlay specifics
        public string WhitelabelId { get; set; }
        public string AffiliateId { get; set; }
        public string Tracker { get; set; }
        
        // For custom properties that can be added dynamically
        public Dictionary<string, string> CustomProperties { get; set; } = new Dictionary<string, string>();
        
        // Navigation properties
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<UserLoginHistory> LoginHistory { get; set; } = new List<UserLoginHistory>();
    }

    public class Role
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string NormalizedName { get; set; }
        public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();
        public string Description { get; set; }
        
        // Navigation properties
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<RoleClaim> RoleClaims { get; set; } = new List<RoleClaim>();
    }

    public class UserRole
    {
        public string UserId { get; set; }
        public string RoleId { get; set; }
        
        // Navigation properties
        public ApplicationUser User { get; set; }
        public Role Role { get; set; }
    }

    public class RoleClaim
    {
        public int Id { get; set; }
        public string RoleId { get; set; }
        public string ClaimType { get; set; }
        public string ClaimValue { get; set; }
        
        // Navigation properties
        public Role Role { get; set; }
    }

    public class UserClaim
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string ClaimType { get; set; }
        public string ClaimValue { get; set; }
        
        // Navigation properties
        public ApplicationUser User { get; set; }
    }

    public class RefreshToken
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Token { get; set; } = Guid.NewGuid().ToString();
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime Expires { get; set; }
        public string CreatedByIp { get; set; }
        public DateTime? Revoked { get; set; }
        public string RevokedByIp { get; set; }
        public string ReplacedByToken { get; set; }
        public string ReasonRevoked { get; set; }
        public bool IsExpired => DateTime.UtcNow >= Expires;
        public bool IsRevoked => Revoked != null;
        public bool IsActive => !IsRevoked && !IsExpired;
        
        // Navigation properties
        public ApplicationUser User { get; set; }
    }

    public class UserLoginHistory
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public DateTime LoginTime { get; set; } = DateTime.UtcNow;
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string DeviceInfo { get; set; }
        public bool Success { get; set; }
        public string FailureReason { get; set; }
        
        // Navigation properties
        public ApplicationUser User { get; set; }
    }

    public class AuditLog
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Action { get; set; }
        public string EntityName { get; set; }
        public string EntityId { get; set; }
        public string OldValues { get; set; }
        public string NewValues { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string IpAddress { get; set; }
    }

    #endregion

    #region Database Context

    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RoleClaim> RoleClaims { get; set; }
        public DbSet<UserClaim> UserClaims { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<UserLoginHistory> UserLoginHistory { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure primary keys
            modelBuilder.Entity<ApplicationUser>().HasKey(u => u.Id);
            modelBuilder.Entity<Role>().HasKey(r => r.Id);
            modelBuilder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });
            modelBuilder.Entity<RoleClaim>().HasKey(rc => rc.Id);
            modelBuilder.Entity<UserClaim>().HasKey(uc => uc.Id);
            modelBuilder.Entity<RefreshToken>().HasKey(rt => rt.Id);
            modelBuilder.Entity<UserLoginHistory>().HasKey(lh => lh.Id);
            modelBuilder.Entity<AuditLog>().HasKey(a => a.Id);

            // Configure relationships
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RoleClaim>()
                .HasOne(rc => rc.Role)
                .WithMany(r => r.RoleClaims)
                .HasForeignKey(rc => rc.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserClaim>()
                .HasOne(uc => uc.User)
                .WithMany()
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserLoginHistory>()
                .HasOne(lh => lh.User)
                .WithMany(u => u.LoginHistory)
                .HasForeignKey(lh => lh.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure indexes
            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.UserName)
                .IsUnique();

            modelBuilder.Entity<Role>()
                .HasIndex(r => r.Name)
                .IsUnique();

            // Convert dictionary to JSON for CustomProperties
            modelBuilder.Entity<ApplicationUser>()
                .Property(u => u.CustomProperties)
                .HasJsonConversion();

            // Seed initial roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = "1", Name = "Admin", NormalizedName = "ADMIN", Description = "System administrator with full access" },
                new Role { Id = "2", Name = "Partner", NormalizedName = "PARTNER", Description = "Partner with access to their whitelabels" },
                new Role { Id = "3", Name = "Subpartner", NormalizedName = "SUBPARTNER", Description = "Subpartner with access to a single whitelabel with specific tracker" }
            );
        }
    }

    // Extension method for Entity Framework
    public static class ModelBuilderExtensions
    {
        public static PropertyBuilder<Dictionary<string, string>> HasJsonConversion(this PropertyBuilder<Dictionary<string, string>> propertyBuilder)
        {
            // This extension method would normally use EF's JSON conversion functionality
            // For brevity, implementation details are omitted
            return propertyBuilder;
        }
    }

    #endregion

    #region Settings

    public class JwtSettings
    {
        public string Secret { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int AccessTokenExpirationMinutes { get; set; } = 15;
        public int RefreshTokenExpirationDays { get; set; } = 7;
    }

    public class PasswordPolicySettings
    {
        public int MinimumLength { get; set; } = 8;
        public bool RequireDigit { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireSpecialCharacter { get; set; } = true;
        public int PasswordExpirationDays { get; set; } = 90;
        public int PasswordHistoryCount { get; set; } = 5;
        public int MaxFailedAttempts { get; set; } = 5;
        public int LockoutDurationMinutes { get; set; } = 15;
    }

    public class UserOptions
    {
        public bool RequireEmailConfirmation { get; set; } = true;
        public bool RequirePhoneConfirmation { get; set; } = false;
        public bool EnableTwoFactorByDefault { get; set; } = false;
        public List<string> CustomUserFields { get; set; } = new List<string>();
    }

    #endregion

    #region DTOs

    // Authentication DTOs
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
        public string TwoFactorCode { get; set; }
        public string TwoFactorRecoveryCode { get; set; }
    }

    public class LoginResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime Expiration { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public bool RequiresTwoFactor { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; }
    }

    public class TokenResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime Expiration { get; set; }
    }

    // User DTOs
    public class RegisterUserRequest
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string WhitelabelId { get; set; }
        public string AffiliateId { get; set; }
        public string Tracker { get; set; }
        public Dictionary<string, string> CustomProperties { get; set; } = new Dictionary<string, string>();
    }

    public class UserResponse
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; }
        public string WhitelabelId { get; set; }
        public string AffiliateId { get; set; }
        public string Tracker { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public Dictionary<string, string> CustomProperties { get; set; } = new Dictionary<string, string>();
    }

    public class UpdateUserRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string WhitelabelId { get; set; }
        public string AffiliateId { get; set; }
        public string Tracker { get; set; }
        public Dictionary<string, string> CustomProperties { get; set; } = new Dictionary<string, string>();
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }

    // Role DTOs
    public class RoleResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<ClaimResponse> Claims { get; set; } = new List<ClaimResponse>();
    }

    public class CreateRoleRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
    }

    public class UpdateRoleRequest
    {
        public string Description { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
    }

    // Claim DTOs
    public class ClaimResponse
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }

    // Two-Factor Authentication DTOs
    public class EnableTwoFactorRequest
    {
        public string Password { get; set; }
    }

    public class VerifyTwoFactorRequest
    {
        public string Code { get; set; }
    }

    public class TwoFactorResponse
    {
        public string SharedKey { get; set; }
        public string AuthenticatorUri { get; set; }
        public List<string> RecoveryCodes { get; set; } = new List<string>();
    }

    // User Management DTOs
    public class AssignRoleRequest
    {
        public string UserId { get; set; }
        public string RoleName { get; set; }
    }

    public class RemoveRoleRequest
    {
        public string UserId { get; set; }
        public string RoleName { get; set; }
    }

    public class UserFilterRequest
    {
        public string SearchTerm { get; set; }
        public string WhitelabelId { get; set; }
        public string AffiliateId { get; set; }
        public string Tracker { get; set; }
        public string RoleName { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public DateTime? LastLoginFrom { get; set; }
        public DateTime? LastLoginTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string OrderBy { get; set; } = "CreatedAt";
        public bool OrderByDescending { get; set; } = true;
    }

    public class PagedResponse<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
        public List<T> Items { get; set; } = new List<T>();
    }

    #endregion

    #region Services

    // Service Interfaces
    public interface IUserService
    {
        Task<UserResponse> GetUserById(string id);
        Task<UserResponse> GetUserByUsername(string username);
        Task<UserResponse> GetUserByEmail(string email);
        Task<PagedResponse<UserResponse>> GetUsers(UserFilterRequest filter);
        Task<UserResponse> CreateUser(RegisterUserRequest request, string createdByUserId);
        Task<UserResponse> UpdateUser(string id, UpdateUserRequest request);
        Task<bool> DeleteUser(string id);
        Task<bool> DeactivateUser(string id);
        Task<bool> ActivateUser(string id);
        Task<bool> ConfirmEmail(string userId, string token);
        Task<bool> ConfirmPhoneNumber(string userId, string token);
    }

    public interface IAuthService
    {
        Task<LoginResponse> Login(LoginRequest request, string ipAddress, string userAgent);
        Task<TokenResponse> RefreshToken(string refreshToken, string ipAddress);
        Task<bool> RevokeToken(string refreshToken, string ipAddress);
        Task<bool> ForgotPassword(ForgotPasswordRequest request);
        Task<bool> ResetPassword(ResetPasswordRequest request);
        Task<bool> ChangePassword(string userId, ChangePasswordRequest request);
        Task<bool> Logout(string userId);
    }

    public interface IRoleService
    {
        Task<List<RoleResponse>> GetAllRoles();
        Task<RoleResponse> GetRoleById(string id);
        Task<RoleResponse> GetRoleByName(string name);
        Task<RoleResponse> CreateRole(CreateRoleRequest request);
        Task<RoleResponse> UpdateRole(string id, UpdateRoleRequest request);
        Task<bool> DeleteRole(string id);
        Task<bool> AssignRoleToUser(AssignRoleRequest request);
        Task<bool> RemoveRoleFromUser(RemoveRoleRequest request);
        Task<List<ClaimResponse>> GetRoleClaims(string roleId);
        Task<bool> AddClaimToRole(string roleId, string claimType, string claimValue);
        Task<bool> RemoveClaimFromRole(string roleId, string claimType, string claimValue);
    }

    public interface ITwoFactorService
    {
        Task<TwoFactorResponse> EnableTwoFactor(string userId, EnableTwoFactorRequest request);
        Task<bool> VerifyTwoFactor(string userId, VerifyTwoFactorRequest request);
        Task<bool> DisableTwoFactor(string userId, string password);
        Task<List<string>> GenerateRecoveryCodes(string userId);
        Task<bool> VerifyTwoFactorToken(string userId, string token);
        Task<bool> RedeemTwoFactorRecoveryCode(string userId, string recoveryCode);
    }

    public interface IAuditService
    {
        Task LogAction(string userId, string action, string entityName, string entityId, string oldValues, string newValues, string ipAddress);
        Task<PagedResponse<AuditLog>> GetAuditLogs(string userId, DateTime? fromDate, DateTime? toDate, string action, int page, int pageSize);
    }

    public interface ITokenService
    {
        Task<string> GenerateAccessToken(ApplicationUser user);
        Task<RefreshToken> GenerateRefreshToken(string userId, string ipAddress);
        Task<bool> ValidateToken(string token);
        Task<ClaimsPrincipal> GetPrincipalFromToken(string token);
        Task<bool> BlacklistToken(string token, DateTime expiration);
        Task<bool> IsTokenBlacklisted(string token);
    }

    public interface IPasswordService
    {
        Task<bool> ValidatePassword(string password);
        Task<bool> IsPasswordExpired(string userId);
        Task<bool> CheckPasswordHistory(string userId, string newPasswordHash);
        Task AddPasswordToHistory(string userId, string passwordHash);
        Task<string> GenerateSecurePassword();
    }

    // Service Implementations
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserService> _logger;
        private readonly IAuditService _auditService;

        public UserService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<UserService> logger,
            IAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _auditService = auditService;
        }

        public async Task<UserResponse> GetUserById(string id)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return null;

            return MapToUserResponse(user);
        }

        public async Task<UserResponse> GetUserByUsername(string username)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserName.ToLower() == username.ToLower());

            if (user == null)
                return null;

            return MapToUserResponse(user);
        }

        public async Task<UserResponse> GetUserByEmail(string email)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpper());

            if (user == null)
                return null;

            return MapToUserResponse(user);
        }

        public async Task<PagedResponse<UserResponse>> GetUsers(UserFilterRequest filter)
        {
            IQueryable<ApplicationUser> query = _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role);

            // Apply filters
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(u =>
                    u.UserName.Contains(filter.SearchTerm) ||
                    u.Email.Contains(filter.SearchTerm) ||
                    u.FirstName.Contains(filter.SearchTerm) ||
                    u.LastName.Contains(filter.SearchTerm));
            }

            if (!string.IsNullOrEmpty(filter.WhitelabelId))
            {
                query = query.Where(u => u.WhitelabelId == filter.WhitelabelId);
            }

            if (!string.IsNullOrEmpty(filter.AffiliateId))
            {
                query = query.Where(u => u.AffiliateId == filter.AffiliateId);
            }

            if (!string.IsNullOrEmpty(filter.Tracker))
            {
                query = query.Where(u => u.Tracker == filter.Tracker);
            }

            if (!string.IsNullOrEmpty(filter.RoleName))
            {
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == filter.RoleName));
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == filter.IsActive.Value);
            }

            if (filter.CreatedFrom.HasValue)
            {
                query = query.Where(u => u.CreatedAt >= filter.CreatedFrom.Value);
            }

            if (filter.CreatedTo.HasValue)
            {
                query = query.Where(u => u.CreatedAt <= filter.CreatedTo.Value);
            }

            if (filter.LastLoginFrom.HasValue)
            {
                query = query.Where(u => u.LastLogin >= filter.LastLoginFrom.Value);
            }

            if (filter.LastLoginTo.HasValue)
            {
                query = query.Where(u => u.LastLogin <= filter.LastLoginTo.Value);
            }

            // Apply ordering
            if (filter.OrderByDescending)
            {
                query = filter.OrderBy.ToLower() switch
                {
                    "username" => query.OrderByDescending(u => u.UserName),
                    "email" => query.OrderByDescending(u => u.Email),
                    "firstname" => query.OrderByDescending(u => u.FirstName),
                    "lastname" => query.OrderByDescending(u => u.LastName),
                    "lastlogin" => query.OrderByDescending(u => u.LastLogin),
                    _ => query.OrderByDescending(u => u.CreatedAt)
                };
            }
            else
            {
                query = filter.OrderBy.ToLower() switch
                {
                    "username" => query.OrderBy(u => u.UserName),
                    "email" => query.OrderBy(u => u.Email),
                    "firstname" => query.OrderBy(u => u.FirstName),
                    "lastname" => query.OrderBy(u => u.LastName),
                    "lastlogin" => query.OrderBy(u => u.LastLogin),
                    _ => query.OrderBy(u => u.CreatedAt)
                };
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var users = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // Map to response
            var userResponses = users.Select(MapToUserResponse).ToList();

            return new PagedResponse<UserResponse>
            {
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount,
                Items = userResponses
            };
        }

        public async Task<UserResponse> CreateUser(RegisterUserRequest request, string createdByUserId)
        {
            // Create new user
            var user = new ApplicationUser
            {
                UserName = request.Username,
                Email = request.Email,
                NormalizedEmail = request.Email.ToUpper(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                WhitelabelId = request.WhitelabelId,
                AffiliateId = request.AffiliateId,
                Tracker = request.Tracker,
                CustomProperties = request.CustomProperties ?? new Dictionary<string, string>()
            };

            // Hash the password
            user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, request.Password);

            // Save to database
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Log the action
            await _auditService.LogAction(
                createdByUserId,
                "Create",
                "User",
                user.Id,
                null,
                System.Text.Json.JsonSerializer.Serialize(new { user.Id, user.UserName, user.Email }),
                null);

            return MapToUserResponse(user);
        }

        public async Task<UserResponse> UpdateUser(string id, UpdateUserRequest request)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return null;

            // Store old values for audit
            var oldValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.WhitelabelId,
                user.AffiliateId,
                user.Tracker,
                user.CustomProperties
            });

            // Update user properties
            user.FirstName = request.FirstName ?? user.FirstName;
            user.LastName = request.LastName ?? user.LastName;
            user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
            user.WhitelabelId = request.WhitelabelId ?? user.WhitelabelId;
            user.AffiliateId = request.AffiliateId ?? user.AffiliateId;
            user.Tracker = request.Tracker ?? user.Tracker;

            // Update custom properties
            if (request.CustomProperties != null)
            {
                foreach (var prop in request.CustomProperties)
                {
                    if (user.CustomProperties.ContainsKey(prop.Key))
                        user.CustomProperties[prop.Key] = prop.Value;
                    else
                        user.CustomProperties.Add(prop.Key, prop.Value);
                }
            }

            // Save changes
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Log the action
            await _auditService.LogAction(
                id, // Assuming the logged-in user is making the update
                "Update",
                "User",
                user.Id,
                oldValues,
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    user.FirstName,
                    user.LastName,
                    user.PhoneNumber,
                    user.WhitelabelId,
                    user.AffiliateId,
                    user.Tracker,
                    user.CustomProperties
                }),
                null);

            return MapToUserResponse(user);
        }

        public async Task<bool> DeleteUser(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return false;

            // Soft delete - mark as inactive
            user.IsActive = false;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Log the action
            await _auditService.LogAction(
                id, // Assuming the logged-in user is making the deletion
                "Delete",
                "User",
                user.Id,
                System.Text.Json.JsonSerializer.Serialize(new { user.Id, user.UserName, user.Email, IsActive = true }),
                System.Text.Json.JsonSerializer.Serialize(new { user.Id, user.UserName, user.Email, IsActive = false }),
                null);

            return true;
        }

        public async Task<bool> DeactivateUser(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || !user.IsActive)
                return false;

            user.IsActive = false;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Log the action
            await _auditService.LogAction(
                id, // Assuming the logged-in user is making the deactivation
                "Deactivate",
                "User",
                user.Id,
                System.Text.Json.JsonSerializer.Serialize(new { IsActive = true }),
                System.Text.Json.JsonSerializer.Serialize(new { IsActive = false }),
                null);

            return true;
        }

        public async Task<bool> ActivateUser(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.IsActive)
                return false;

            user.IsActive = true;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Log the action
            await _auditService.LogAction(
                id, // Assuming the logged-in user is making the activation
                "Activate",
                "User",
                user.Id,
                System.Text.Json.JsonSerializer.Serialize(new { IsActive = false }),
                System.Text.Json.JsonSerializer.Serialize(new { IsActive = true }),
                null);

            return true;
        }

        public async Task<bool> ConfirmEmail(string userId, string token)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            // In a real implementation, validate the token
            // For brevity, we're assuming the token is valid

            user.EmailConfirmed = true;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Log the action
            await _auditService.LogAction(
                userId,
                "ConfirmEmail",
                "User",
                user.Id,
                System.Text.Json.JsonSerializer.Serialize(new { EmailConfirmed = false }),
                System.Text.Json.JsonSerializer.Serialize(new { EmailConfirmed = true }),
                null);

            return true;
        }

        public async Task<bool> ConfirmPhoneNumber(string userId, string token)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            // In a real implementation, validate the token
            // For brevity, we're assuming the token is valid

            user.PhoneNumberConfirmed = true;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Log the action
            await _auditService.LogAction(
                userId,
                "ConfirmPhoneNumber",
                "User",
                user.Id,
                System.Text.Json.JsonSerializer.Serialize(new { PhoneNumberConfirmed = false }),
                System.Text.Json.JsonSerializer.Serialize(new { PhoneNumberConfirmed = true }),
                null);

            return true;
        }

        private UserResponse MapToUserResponse(ApplicationUser user)
        {
            return new UserResponse
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                EmailConfirmed = user.EmailConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin,
                IsActive = user.IsActive,
                WhitelabelId = user.WhitelabelId,
                AffiliateId = user.AffiliateId,
                Tracker = user.Tracker,
                Roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>(),
                CustomProperties = user.CustomProperties
            };
        }
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IUserService _userService;
        private readonly IAuditService _auditService;
        private readonly IPasswordService _passwordService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            ApplicationDbContext context,
            ITokenService tokenService,
            IUserService userService,
            IAuditService auditService,
            IPasswordService passwordService,
            UserManager<ApplicationUser> userManager,
            ILogger<AuthService> logger)
        {
            _context = context;
            _tokenService = tokenService;
            _userService = userService;
            _auditService = auditService;
            _passwordService = passwordService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<LoginResponse> Login(LoginRequest request, string ipAddress, string userAgent)
        {
            // Find user by username
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserName.ToLower() == request.Username.ToLower());

            // Track login attempt
            var loginHistory = new UserLoginHistory
            {
                UserId = user?.Id,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Success = false
            };

            if (user == null)
            {
                loginHistory.FailureReason = "User not found";
                await _context.UserLoginHistory.AddAsync(loginHistory);
                await _context.SaveChangesAsync();
                return null;
            }

            // Check if user is active
            if (!user.IsActive)
            {
                loginHistory.FailureReason = "User is inactive";
                await _context.UserLoginHistory.AddAsync(loginHistory);
                await _context.SaveChangesAsync();
                return null;
            }

            // Check if account is locked out
            if (user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                loginHistory.FailureReason = "User is locked out";
                await _context.UserLoginHistory.AddAsync(loginHistory);
                await _context.SaveChangesAsync();
                return null;
            }

            // Verify password
            var passwordVerificationResult = _userManager.PasswordHasher.VerifyHashedPassword(
                user, user.PasswordHash, request.Password);

            if (passwordVerificationResult == PasswordVerificationResult.Failed)
            {
                // Increment failed access count
                user.AccessFailedCount++;
                
                // Check if we should lock out the account
                if (user.AccessFailedCount >= 5) // Using a hardcoded value for simplicity
                {
                    user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15); // 15 minutes lockout
                    loginHistory.FailureReason = "Account locked due to too many failed attempts";
                }
                else
                {
                    loginHistory.FailureReason = "Invalid password";
                }

                _context.Users.Update(user);
                await _context.UserLoginHistory.AddAsync(loginHistory);
                await _context.SaveChangesAsync();
                return null;
            }

            // Check if two-factor is required
            if (user.TwoFactorEnabled && string.IsNullOrEmpty(request.TwoFactorCode))
            {
                return new LoginResponse
                {
                    RequiresTwoFactor = true,
                    UserId = user.Id,
                    Username = user.UserName
                };
            }

            // Verify two-factor code if required
            if (user.TwoFactorEnabled)
            {
                // In a real implementation, validate the two-factor code
                // For brevity, we're assuming it's valid if it's "123456" (FOR DEMO ONLY!)
                if (request.TwoFactorCode != "123456")
                {
                    loginHistory.FailureReason = "Invalid two-factor code";
                    await _context.UserLoginHistory.AddAsync(loginHistory);
                    await _context.SaveChangesAsync();
                    return null;
                }
            }

            // Reset failed access count
            user.AccessFailedCount = 0;
            
            // Update last login
            user.LastLogin = DateTime.UtcNow;
            
            // Generate tokens
            var accessToken = await _tokenService.GenerateAccessToken(user);
            var refreshToken = await _tokenService.GenerateRefreshToken(user.Id, ipAddress);
            
            // Add refresh token to user
            user.RefreshTokens.Add(refreshToken);
            
            // Update user
            _context.Users.Update(user);
            
            // Record successful login
            loginHistory.Success = true;
            await _context.UserLoginHistory.AddAsync(loginHistory);
            
            await _context.SaveChangesAsync();

            // Check if password is expired
            var isPasswordExpired = await _passwordService.IsPasswordExpired(user.Id);

            // Create response
            var response = new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                Expiration = DateTime.UtcNow.AddMinutes(15), // Assuming 15 min token lifetime
                UserId = user.Id,
                Username = user.UserName,
                RequiresTwoFactor = false,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
            };

            return response;
        }

        public async Task<TokenResponse> RefreshToken(string refreshToken, string ipAddress)
        {
            // Find the refresh token
            var token = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (token == null)
                return null;

            // Check if the token is active
            if (!token.IsActive)
                return null;

            // Check if the user is active
            if (!token.User.IsActive)
                return null;

            // Revoke the current refresh token
            token.Revoked = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            token.ReasonRevoked = "Replaced by new token";

            // Generate new tokens
            var newAccessToken = await _tokenService.GenerateAccessToken(token.User);
            var newRefreshToken = await _tokenService.GenerateRefreshToken(token.User.Id, ipAddress);
            
            // Set the replaced by token
            token.ReplacedByToken = newRefreshToken.Token;
            
            // Add the new refresh token to the user
            token.User.RefreshTokens.Add(newRefreshToken);
            
            // Update in database
            _context.RefreshTokens.Update(token);
            await _context.SaveChangesAsync();

            return new TokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                Expiration = DateTime.UtcNow.AddMinutes(15) // Assuming 15 min token lifetime
            };
        }

        public async Task<bool> RevokeToken(string refreshToken, string ipAddress)
        {
            var token = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (token == null || !token.IsActive)
                return false;

            // Revoke the token
            token.Revoked = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            token.ReasonRevoked = "Revoked without replacement";

            _context.RefreshTokens.Update(token);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ForgotPassword(ForgotPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (user == null)
                return false;

            // In a real implementation, generate a password reset token and send email
            // For brevity, we're just returning true

            // Log the action
            await _auditService.LogAction(
                user.Id,
                "ForgotPassword",
                "User",
                user.Id,
                null,
                null,
                null);

            return true;
        }

        public async Task<bool> ResetPassword(ResetPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (user == null)
                return false;

            // In a real implementation, validate the token
            // For brevity, we're assuming it's valid

            // Check if the new password meets the requirements
            if (!await _passwordService.ValidatePassword(request.NewPassword))
                return false;

            // Check if the new password is in the history
            var newPasswordHash = _userManager.PasswordHasher.HashPassword(user, request.NewPassword);
            if (await _passwordService.CheckPasswordHistory(user.Id, newPasswordHash))
                return false;

            // Update the password
            user.PasswordHash = newPasswordHash;
            
            // Add to password history
            await _passwordService.AddPasswordToHistory(user.Id, newPasswordHash);

            // Update user
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Log the action
            await _auditService.LogAction(
                user.Id,
                "ResetPassword",
                "User",
                user.Id,
                null,
                null,
                null);

            return true;
        }

        public async Task<bool> ChangePassword(string userId, ChangePasswordRequest request)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return false;

            // Verify current password
            var passwordVerificationResult = _userManager.PasswordHasher.VerifyHashedPassword(
                user, user.PasswordHash, request.CurrentPassword);

            if (passwordVerificationResult == PasswordVerificationResult.Failed)
                return false;

            // Check if new password meets requirements
            if (!await _passwordService.ValidatePassword(request.NewPassword))
                return false;

            // Check if new password is in history
            var newPasswordHash = _userManager.PasswordHasher.HashPassword(user, request.NewPassword);
            if (await _passwordService.CheckPasswordHistory(user.Id, newPasswordHash))
                return false;

            // Update password
            user.PasswordHash = newPasswordHash;
            
            // Add to password history
            await _passwordService.AddPasswordToHistory(user.Id, newPasswordHash);

            // Update user
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Log the action
            await _auditService.LogAction(
                userId,
                "ChangePassword",
                "User",
                user.Id,
                null,
                null,
                null);

            return true;
        }

        public async Task<bool> Logout(string userId)
        {
            // Revoke all active refresh tokens for the user
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.IsActive)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.Revoked = DateTime.UtcNow;
                token.ReasonRevoked = "Logout";
            }

            _context.RefreshTokens.UpdateRange(tokens);
            await _context.SaveChangesAsync();

            // Log the action
            await _auditService.LogAction(
                userId,
                "Logout",
                "User",
                userId,
                null,
                null,
                null);

            return true;
        }
    }

    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly IDistributedCache _cache;
        private readonly ILogger<TokenService> _logger;

        public TokenService(
            IOptions<JwtSettings> jwtSettings,
            IDistributedCache cache,
            ILogger<TokenService> logger)
        {
            _jwtSettings = jwtSettings.Value;
            _cache = cache;
            _logger = logger;
        }

        public async Task<string> GenerateAccessToken(ApplicationUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

            // Create claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Add role claims
            if (user.UserRoles != null)
            {
                foreach (var userRole in user.UserRoles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
                }
            }

            // Add custom claims
            if (!string.IsNullOrEmpty(user.WhitelabelId))
                claims.Add(new Claim("WhitelabelId", user.WhitelabelId));

            if (!string.IsNullOrEmpty(user.AffiliateId))
                claims.Add(new Claim("AffiliateId", user.AffiliateId));

            if (!string.IsNullOrEmpty(user.Tracker))
                claims.Add(new Claim("Tracker", user.Tracker));

            // Create token descriptor
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            // Create token
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task<RefreshToken> GenerateRefreshToken(string userId, string ipAddress)
        {
            // Create refresh token
            var refreshToken = new RefreshToken
            {
                UserId = userId,
                Token = Guid.NewGuid().ToString(),
                Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                Created = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };

            return refreshToken;
        }

        public async Task<bool> ValidateToken(string token)
        {
            // Check if token is blacklisted
            if (await IsTokenBlacklisted(token))
                return false;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return false;
            }
        }

        public async Task<ClaimsPrincipal> GetPrincipalFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = false, // Don't validate lifetime here
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);
                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Getting principal from token failed");
                return null;
            }
        }

        public async Task<bool> BlacklistToken(string token, DateTime expiration)
        {
            try
            {
                // Calculate the remaining time until expiration
                var timeToExpiration = expiration - DateTime.UtcNow;
                if (timeToExpiration <= TimeSpan.Zero)
                    return true; // Token already expired

                // Add token to blacklist cache
                await _cache.SetStringAsync(
                    $"blacklist:{token}",
                    "revoked",
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = timeToExpiration
                    });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Blacklisting token failed");
                return false;
            }
        }

        public async Task<bool> IsTokenBlacklisted(string token)
        {
            try
            {
                var cachedValue = await _cache.GetStringAsync($"blacklist:{token}");
                return cachedValue != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Checking blacklisted token failed");
                return false;
            }
        }
    }

    public class RoleService : IRoleService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ILogger<RoleService> _logger;

        public RoleService(
            ApplicationDbContext context,
            IAuditService auditService,
            ILogger<RoleService> logger)
        {
            _context = context;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<List<RoleResponse>> GetAllRoles()
        {
            var roles = await _context.Roles
                .Include(r => r.RoleClaims)
                .ToListAsync();

            return roles.Select(MapToRoleResponse).ToList();
        }

        public async Task<RoleResponse> GetRoleById(string id)
        {
            var role = await _context.Roles
                .Include(r => r.RoleClaims)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null)
                return null;

            return MapToRoleResponse(role);
        }

        public async Task<RoleResponse> GetRoleByName(string name)
        {
            var role = await _context.Roles
                .Include(r => r.RoleClaims)
                .FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower());

            if (role == null)
                return null;

            return MapToRoleResponse(role);
        }

        public async Task<RoleResponse> CreateRole(CreateRoleRequest request)
        {
            // Check if role already exists
            if (await _context.Roles.AnyAsync(r => r.Name.ToLower() == request.Name.ToLower()))
                return null;

            // Create new role
            var role = new Role
            {
                Name = request.Name,
                NormalizedName = request.Name.ToUpper(),
                Description = request.Description
            };

            // Add role claims
            if (request.Permissions != null && request.Permissions.Any())
            {
                foreach (var permission in request.Permissions)
                {
                    role.RoleClaims.Add(new RoleClaim
                    {
                        RoleId = role.Id,
                        ClaimType = "Permission",
                        ClaimValue = permission
                    });
                }
            }

            // Save to database
            await _context.Roles.AddAsync(role);
            await _context.SaveChangesAsync();

            // Log the action
            await _auditService.LogAction(
                null, // User ID would come from the current user context
                "Create",
                "Role",
                role.Id,
                null,
                System.Text.Json.JsonSerializer.Serialize(new { role.Id, role.Name, role.Description }),
                null);

            return MapToRoleResponse(role);
        }

        public async Task<RoleResponse> UpdateRole(string id, UpdateRoleRequest request)
        {
            var role = await _context.Roles
                .Include(r => r.RoleClaims)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null)
                return null;

            // Store old values for audit
            var oldValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                role.Description,
                Permissions = role.RoleClaims.Select(rc => rc.ClaimValue).ToList()
            });

            // Update role properties
            role.Description = request.Description ?? role.Description;

            // Update permissions
            if (request.Permissions != null)
            {
                // Remove existing claims
                _context.RoleClaims.RemoveRange(role.RoleClaims);

                // Add new claims
                foreach (var permission in request.Permissions)
                {
                    role.RoleClaims.Add(new RoleClaim
                    {
                        RoleId = role.Id,
                        ClaimType = "Permission",
                        ClaimValue = permission
                    });
                }
            }

            // Save changes
            _context.Roles.Update(role);
            await _context.SaveChangesAsync();

            // Log the action
            await _auditService.LogAction(
                null, // User ID would come from the current user context
                "Update",
                "Role",
                role.Id,
                oldValues,
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    role.Description,
                    Permissions = role.RoleClaims.Select(rc => rc.ClaimValue).ToList()
                }),
                null);

            return MapToRoleResponse(role);
        }

        public async Task<bool> DeleteRole(string id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
                return false;

            // Check if role is in use
            if (await _context.UserRoles.AnyAsync(ur => ur.RoleId == id))
                return false;

            // Delete role
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            // Log the action
            await _auditService.LogAction(
                null, // User ID would come from the current user context
                "Delete",
                "Role",
                role.Id,
                System.Text.Json.JsonSerializer.Serialize(new { role.Id, role.Name, role.Description }),
                null,
                null);

            return true;
        }

        public async Task<bool> AssignRoleToUser(AssignRoleRequest request)
        {
            // Check if user exists
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
                return false;

            // Check if role exists
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == request.RoleName.ToLower());
            if (role == null)
                return false;

            // Check if user already has this role
            if (await _context.UserRoles.AnyAsync(ur => ur.UserId == request.UserId && ur.RoleId == role.Id))
                return false;

            // Add role to user
            var userRole = new UserRole
            {
                UserId = request.UserId,
                RoleId = role.Id
            };

            await _context.UserRoles.AddAsync(userRole);
            await _context.SaveChangesAsync();

            // Log the action
            await _auditService.LogAction(
                null, // User ID would come from the current user context
                "AssignRole",
                "UserRole",
                $"{request.UserId}:{role.Id}",
                null,
                System.Text.Json.JsonSerializer.Serialize(new { UserId = request.UserId, RoleName = request.RoleName }),
                null);

            return true;
        }

        public async Task<bool> RemoveRoleFromUser(RemoveRoleRequest request)
        {
            // Check if user exists
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
                return false;

            // Check if role exists
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == request.RoleName.ToLower());
            if (role == null)
                return false;

            // Check if user has this role
            var userRole = await _context.UserRoles.FirstOrDefaultAsync(ur => 
                ur.UserId == request.UserId && ur.RoleId == role.Id);
                
            if (userRole == null)
                return false;

            // Remove role from user
            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            // Log the action
            await _auditService.LogAction(
                null, // User ID would come from the current user context
                "RemoveRole",
                "UserRole",
                $"{request.UserId}:{role.Id}",
                System.Text.Json.JsonSerializer.Serialize(new { UserId = request.UserId, RoleName = request.RoleName }),
                null,
                null);

            return true;
        }

        public async Task<List<ClaimResponse>> GetRoleClaims(string roleId)
        {
            var claims = await _context.RoleClaims
                .Where(rc => rc.RoleId == roleId)
                .ToListAsync();

            return claims.Select(c => new ClaimResponse
            {
                Type = c.ClaimType,
                Value = c.ClaimValue
            }).ToList();
        }

        public async Task<bool> AddClaimToRole(string roleId, string claimType, string claimValue)
        {
            // Check if role exists
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null)
                return false;

            // Check if claim already exists
            if (await _context.RoleClaims.AnyAsync(rc => 
                rc.RoleId == roleId && rc.ClaimType == claimType && rc.ClaimValue == claimValue))
                return false;

            // Add claim to role
            var roleClaim = new RoleClaim
            {
                RoleId = roleId,
                ClaimType = claimType,
                ClaimValue = claimValue
            };

            await _context.RoleClaims.AddAsync(roleClaim);
            await _context.SaveChangesAsync();

            // Log the action
            await _auditService.LogAction(
                null, // User ID would come from the current user context
                "AddClaim",
                "RoleClaim",
                $"{roleId}:{claimType}:{claimValue}",
                null,
                System.Text.Json.JsonSerializer.Serialize(new { RoleId = roleId, ClaimType = claimType, ClaimValue = claimValue }),
                null);

            return true;
        }

        public async Task<bool> RemoveClaimFromRole(string roleId, string claimType, string claimValue)
        {
            // Check if role exists
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null)
                return false;

            // Find the claim
            var roleClaim = await _context.RoleClaims.FirstOrDefaultAsync(rc => 
                rc.RoleId == roleId && rc.ClaimType == claimType && rc.ClaimValue == claimValue);
                
            if (roleClaim == null)
                return false;

            // Remove claim
            _context.RoleClaims.Remove(roleClaim);
            await _context.SaveChangesAsync();

            // Log the action
            await _auditService.LogAction(
                null, // User ID would come from the current user context
                "RemoveClaim",
                "RoleClaim",
                $"{roleId}:{claimType}:{claimValue}",
                System.Text.Json.JsonSerializer.Serialize(new { RoleId = roleId, ClaimType = claimType, ClaimValue = claimValue }),
                null,
                null);

            return true;
        }

        private RoleResponse MapToRoleResponse(Role role)
        {
            return new RoleResponse
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                Claims = role.RoleClaims?.Select(rc => new ClaimResponse
                {
                    Type = rc.ClaimType,
                    Value = rc.ClaimValue
                }).ToList() ?? new List<ClaimResponse>()
            };
        }
    }

    public class TwoFactorService : ITwoFactorService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TwoFactorService> _logger;

        public TwoFactorService(
            ApplicationDbContext context,
            IAuditService auditService,
            UserManager<ApplicationUser> userManager,
            ILogger<TwoFactorService> logger)
        {
            _context = context;
            _auditService = auditService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<TwoFactorResponse> EnableTwoFactor(string userId, EnableTwoFactorRequest request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return null;

            // Verify password
            var passwordVerificationResult = _userManager.PasswordHasher.VerifyHashedPassword(
                user, user.PasswordHash, request.Password);

            if (passwordVerificationResult == PasswordVerificationResult.Failed)
                return null;

            // Generate shared key and QR code
            // In a real implementation, use a proper TOTP library
            
            // For demonstration, we're just creating a random key
            var sharedKey = GenerateRandomKey();
            
            // Create authenticator URI
            var authenticatorUri = $"otpauth://totp/IdentityService:{user.Email}?secret={sharedKey}&issuer=IdentityService";
            
            // Generate recovery codes
            var recoveryCodes = await GenerateRecoveryCodes(userId);

            // Return response
            return new TwoFactorResponse
            {
                SharedKey = sharedKey,
                AuthenticatorUri = authenticatorUri,
                RecoveryCodes = recoveryCodes
            };
        }

        public async Task<bool> VerifyTwoFactor(string userId, VerifyTwoFactorRequest request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            // In a real implementation, verify the code against the shared key
            // For demonstration, we're just checking if it's "123456"
            if (request.Code != "123456")
                return false;

            // Enable two factor authentication
            user.TwoFactorEnabled = true;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Log the action
            await _auditService.LogAction(
                userId,
                "EnableTwoFactor",
                "User",
                userId,
                System.Text.Json.JsonSerializer.Serialize(new { TwoFactorEnabled = false }),
                System.Text.Json.JsonSerializer.Serialize(new { TwoFactorEnabled = true }),
                null);

            return true;
        }

        public async Task<bool> DisableTwoFactor(string userId, string password)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            // Verify password
            var passwordVerificationResult = _userManager.PasswordHasher.VerifyHashedPassword(
                user, user.PasswordHash, password);

            if (passwordVerificationResult == PasswordVerificationResult.Failed)
                return false;

            // Disable two factor authentication
            user.TwoFactorEnabled = false;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Log the action
            await _auditService.LogAction(
                userId,
                "DisableTwoFactor",
                "User",
                userId,
                System.Text.Json.JsonSerializer.Serialize(new { TwoFactorEnabled = true }),
                System.Text.Json.JsonSerializer.Serialize(new { TwoFactorEnabled = false }),
                null);

            return true;
        }

        public async Task<List<string>> GenerateRecoveryCodes(string userId)
        {
            // In a real implementation, generate and store recovery codes
            // For demonstration, we're just returning some random codes
            return new List<string>
            {
                Guid.NewGuid().ToString("N").Substring(0, 8),
                Guid.NewGuid().ToString("N").Substring(0, 8),
                Guid.NewGuid().ToString("N").Substring(0, 8),
                Guid.NewGuid().ToString("N").Substring(0, 8),
                Guid.NewGuid().ToString("N").Substring(0, 8)
            };
        }

        public async Task<bool> VerifyTwoFactorToken(string userId, string token)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.TwoFactorEnabled)
                return false;

            // In a real implementation, verify the token against the shared key
            // For demonstration, we're just checking if it's "123456"
            return token == "123456";
        }

        public async Task<bool> RedeemTwoFactorRecoveryCode(string userId, string recoveryCode)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.TwoFactorEnabled)
                return false;

            // In a real implementation, check and remove the recovery code
            // For demonstration, we're just returning true
            return true;
        }

        private string GenerateRandomKey()
        {
            // Generate a random key for TOTP
            var key = new byte[20];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return Convert.ToBase64String(key);
        }
    }

    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            ApplicationDbContext context,
            ILogger<AuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogAction(string userId, string action, string entityName, string entityId, string oldValues, string newValues, string ipAddress)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    EntityName = entityName,
                    EntityId = entityId,
                    OldValues = oldValues,
                    NewValues = newValues,
                    IpAddress = ipAddress
                };

                await _context.AuditLogs.AddAsync(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging audit action");
                // Continue execution even if logging fails
            }
        }

        public async Task<PagedResponse<AuditLog>> GetAuditLogs(string userId, DateTime? fromDate, DateTime? toDate, string action, int page, int pageSize)
        {
            IQueryable<AuditLog> query = _context.AuditLogs;

            // Apply filters
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(a => a.UserId == userId);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(a => a.Timestamp <= toDate.Value);
            }

            if (!string.IsNullOrEmpty(action))
            {
                query = query.Where(a => a.Action == action);
            }

            // Apply ordering
            query = query.OrderByDescending(a => a.Timestamp);

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var auditLogs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<AuditLog>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = auditLogs
            };
        }
    }

    public class PasswordService : IPasswordService
    {
        private readonly ApplicationDbContext _context;
        private readonly IOptions<PasswordPolicySettings> _passwordPolicyOptions;
        private readonly ILogger<PasswordService> _logger;

        public PasswordService(
            ApplicationDbContext context,
            IOptions<PasswordPolicySettings> passwordPolicyOptions,
            ILogger<PasswordService> logger)
        {
            _context = context;
            _passwordPolicyOptions = passwordPolicyOptions;
            _logger = logger;
        }

        public async Task<bool> ValidatePassword(string password)
        {
            var policy = _passwordPolicyOptions.Value;

            // Check password length
            if (password.Length < policy.MinimumLength)
                return false;

            // Check for digit
            if (policy.RequireDigit && !password.Any(c => char.IsDigit(c)))
                return false;

            // Check for lowercase
            if (policy.RequireLowercase && !password.Any(c => char.IsLower(c)))
                return false;

            // Check for uppercase
            if (policy.RequireUppercase && !password.Any(c => char.IsUpper(c)))
                return false;

            // Check for special character
            if (policy.RequireSpecialCharacter && password.All(c => char.IsLetterOrDigit(c)))
                return false;

            return true;
        }

        public async Task<bool> IsPasswordExpired(string userId)
        {
            // In a real implementation, check when the password was last changed
            // For demonstration, we're just returning false
            return false;
        }

        public async Task<bool> CheckPasswordHistory(string userId, string newPasswordHash)
        {
            // In a real implementation, check the password history
            // For demonstration, we're just returning false
            return false;
        }

        public async Task AddPasswordToHistory(string userId, string passwordHash)
        {
            // In a real implementation, add the password to history
            // For demonstration, we're not doing anything
        }

        public async Task<string> GenerateSecurePassword()
        {
            // Generate a secure random password
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string special = "!@#$%^&*()_+-=[]{}|;:,.<>?";

            var random = new Random();
            var password = new StringBuilder();

            // Ensure at least one of each required character type
            password.Append(upper[random.Next(0, upper.Length)]);
            password.Append(lower[random.Next(0, lower.Length)]);
            password.Append(digits[random.Next(0, digits.Length)]);
            password.Append(special[random.Next(0, special.Length)]);

            // Fill the rest with random characters
            var allChars = upper + lower + digits + special;
            for (int i = 4; i < 16; i++)
            {
                password.Append(allChars[random.Next(0, allChars.Length)]);
            }

            // Shuffle the password
            var passwordArray = password.ToString().ToCharArray();
            for (int i = 0; i < passwordArray.Length; i++)
            {
                int j = random.Next(i, passwordArray.Length);
                var temp = passwordArray[i];
                passwordArray[i] = passwordArray[j];
                passwordArray[j] = temp;
            }

            return new string(passwordArray);
        }
    }

    #endregion

    #region Controllers

    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly ITwoFactorService _twoFactorService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            IUserService userService,
            ITwoFactorService twoFactorService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _userService = userService;
            _twoFactorService = twoFactorService;
            _logger = logger;
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Get client info
            var ipAddress = GetIpAddress();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var result = await _authService.Login(request, ipAddress, userAgent);
            if (result == null)
                return Unauthorized(new { message = "Username or password is incorrect" });

            if (result.RequiresTwoFactor)
                return Ok(new { requiresTwoFactor = true, userId = result.UserId });

            return Ok(result);
        }

        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var ipAddress = GetIpAddress();
            var result = await _authService.RefreshToken(request.RefreshToken, ipAddress);
            if (result == null)
                return Unauthorized(new { message = "Invalid token" });

            return Ok(result);
        }

        [HttpPost("revoke-token")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request)
        {
            var ipAddress = GetIpAddress();
            var success = await _authService.RevokeToken(request.RefreshToken, ipAddress);
            if (!success)
                return BadRequest(new { message = "Token is invalid" });

            return Ok(new { message = "Token revoked" });
        }

        [HttpPost("forgot-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await _authService.ForgotPassword(request);
            
            // Always return OK for security reasons
            return Ok(new { message = "If the email exists, a password reset link has been sent" });
        }

        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (request.NewPassword != request.ConfirmPassword)
                return BadRequest(new { message = "Passwords do not match" });

            var success = await _authService.ResetPassword(request);
            if (!success)
                return BadRequest(new { message = "Error resetting password" });

            return Ok(new { message = "Password has been reset" });
        }

        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (request.NewPassword != request.ConfirmPassword)
                return BadRequest(new { message = "Passwords do not match" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var success = await _authService.ChangePassword(userId, request);
            if (!success)
                return BadRequest(new { message = "Error changing password" });

            return Ok(new { message = "Password has been changed" });
        }

        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _authService.Logout(userId);
            return Ok(new { message = "Logged out successfully" });
        }

        [HttpPost("two-factor/enable")]
        [Authorize]
        [ProducesResponseType(typeof(TwoFactorResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> EnableTwoFactor([FromBody] EnableTwoFactorRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _twoFactorService.EnableTwoFactor(userId, request);
            if (result == null)
                return BadRequest(new { message = "Failed to enable two-factor authentication" });

            return Ok(result);
        }

        [HttpPost("two-factor/verify")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> VerifyTwoFactor([FromBody] VerifyTwoFactorRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var success = await _twoFactorService.VerifyTwoFactor(userId, request);
            if (!success)
                return BadRequest(new { message = "Invalid verification code" });

            return Ok(new { message = "Two-factor authentication enabled" });
        }

        [HttpPost("two-factor/disable")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DisableTwoFactor([FromBody] EnableTwoFactorRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var success = await _twoFactorService.DisableTwoFactor(userId, request.Password);
            if (!success)
                return BadRequest(new { message = "Failed to disable two-factor authentication" });

            return Ok(new { message = "Two-factor authentication disabled" });
        }

        [HttpGet("two-factor/recovery-codes")]
        [Authorize]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetRecoveryCodes()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var recoveryCodes = await _twoFactorService.GenerateRecoveryCodes(userId);
            return Ok(recoveryCodes);
        }

        private string GetIpAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"];
            else
                return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
        }
    }

    [ApiController]
    [Route("api/v1/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly ILogger<UserController> _logger;

        public UserController(
            IUserService userService,
            IRoleService roleService,
            ILogger<UserController> logger)
        {
            _userService = userService;
            _roleService = roleService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Policy = "RequireAdminRole")]
        [ProducesResponseType(typeof(PagedResponse<UserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUsers([FromQuery] UserFilterRequest filter)
        {
            var users = await _userService.GetUsers(filter);
            return Ok(users);
        }

        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserById(string id)
        {
            // Ensure the user is authorized to view this user
            if (!IsAuthorizedToViewUser(id))
                return Forbid();

            var user = await _userService.GetUserById(id);
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpPost]
        [Authorize(Policy = "RequireAdminRole")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateUser([FromBody] RegisterUserRequest request)
        {
            if (request.Password != request.ConfirmPassword)
                return BadRequest(new { message = "Passwords do not match" });

            var createdByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userService.CreateUser(request, createdByUserId);
            
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
        {
            // Ensure the user is authorized to update this user
            if (!IsAuthorizedToUpdateUser(id))
                return Forbid();

            var user = await _userService.UpdateUser(id, request);
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var success = await _userService.DeleteUser(id);
            if (!success)
                return NotFound();

            return Ok(new { message = "User deleted" });
        }

        [HttpPut("{id}/deactivate")]
        [Authorize(Policy = "RequireAdminRole")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivateUser(string id)
        {
            var success = await _userService.DeactivateUser(id);
            if (!success)
                return NotFound();

            return Ok(new { message = "User deactivated" });
        }

        [HttpPut("{id}/activate")]
        [Authorize(Policy = "RequireAdminRole")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActivateUser(string id)
        {
            var success = await _userService.ActivateUser(id);
            if (!success)
                return NotFound();

            return Ok(new { message = "User activated" });
        }

        [HttpPost("{id}/roles")]
        [Authorize(Policy = "RequireAdminRole")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignRole(string id, [FromBody] string roleName)
        {
            var request = new AssignRoleRequest { UserId = id, RoleName = roleName };
            var success = await _roleService.AssignRoleToUser(request);
            if (!success)
                return BadRequest(new { message = "Failed to assign role" });

            return Ok(new { message = "Role assigned" });
        }

        [HttpDelete("{id}/roles/{roleName}")]
        [Authorize(Policy = "RequireAdminRole")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveRole(string id, string roleName)
        {
            var request = new RemoveRoleRequest { UserId = id, RoleName = roleName };
            var success = await _roleService.RemoveRoleFromUser(request);
            if (!success)
                return BadRequest(new { message = "Failed to remove role" });

            return Ok(new { message = "Role removed" });
        }

        private bool IsAuthorizedToViewUser(string userId)
        {
            // Check if the user is an admin
            if (User.IsInRole("Admin"))
                return true;

            // Check if the user is viewing their own profile
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == userId)
                return true;

            // For partner/subpartner roles, implement WhitelabelAccess policy
            if (User.IsInRole("Partner") || User.IsInRole("Subpartner"))
            {
                // This should be properly handled by the WhitelabelAccessHandler
                // Just a simple check for demonstration
                return true; 
            }

            return false;
        }

        private bool IsAuthorizedToUpdateUser(string userId)
        {
            // Similar to IsAuthorizedToViewUser but more restrictive
            // Check if the user is an admin
            if (User.IsInRole("Admin"))
                return true;

            // Check if the user is updating their own profile
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == userId)
                return true;

            // For partner role, only allow updates to users in their whitelabel
            if (User.IsInRole("Partner"))
            {
                // This should be properly handled by the WhitelabelAccessHandler
                // Just a simple check for demonstration
                return true;
            }

            return false;
        }
    }

    [ApiController]
    [Route("api/v1/roles")]
    [Authorize(Policy = "RequireAdminRole")]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;
        private readonly ILogger<RoleController> _logger;

        public RoleController(
            IRoleService roleService,
            ILogger<RoleController> logger)
        {
            _roleService = roleService;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<RoleResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _roleService.GetAllRoles();
            return Ok(roles);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRoleById(string id)
        {
            var role = await _roleService.GetRoleById(id);
            if (role == null)
                return NotFound();

            return Ok(role);
        }

        [HttpPost]
        [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
        {
            var role = await _roleService.CreateRole(request);
            if (role == null)
                return BadRequest(new { message = "Role already exists" });

            return CreatedAtAction(nameof(GetRoleById), new { id = role.Id }, role);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateRole(string id, [FromBody] UpdateRoleRequest request)
        {
            var role = await _roleService.UpdateRole(id, request);
            if (role == null)
                return NotFound();

            return Ok(role);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteRole(string id)
        {
            var success = await _roleService.DeleteRole(id);
            if (!success)
                return BadRequest(new { message = "Role is in use and cannot be deleted" });

            return Ok(new { message = "Role deleted" });
        }

        [HttpGet("{id}/claims")]
        [ProducesResponseType(typeof(List<ClaimResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRoleClaims(string id)
        {
            // Verify role exists
            var role = await _roleService.GetRoleById(id);
            if (role == null)
                return NotFound();

            var claims = await _roleService.GetRoleClaims(id);
            return Ok(claims);
        }

        [HttpPost("{id}/claims")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddClaimToRole(string id, [FromBody] ClaimResponse claim)
        {
            // Verify role exists
            var role = await _roleService.GetRoleById(id);
            if (role == null)
                return NotFound();

            var success = await _roleService.AddClaimToRole(id, claim.Type, claim.Value);
            if (!success)
                return BadRequest(new { message = "Claim already exists" });

            return Ok(new { message = "Claim added" });
        }

        [HttpDelete("{id}/claims")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveClaimFromRole(string id, [FromBody] ClaimResponse claim)
        {
            // Verify role exists
            var role = await _roleService.GetRoleById(id);
            if (role == null)
                return NotFound();

            var success = await _roleService.RemoveClaimFromRole(id, claim.Type, claim.Value);
            if (!success)
                return BadRequest(new { message = "Claim does not exist" });

            return Ok(new { message = "Claim removed" });
        }
    }

    #endregion

    #region Middleware

    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            var statusCode = StatusCodes.Status500InternalServerError;
            var message = "An error occurred while processing your request.";
            
            if (exception is ValidationException)
            {
                statusCode = StatusCodes.Status400BadRequest;
                message = exception.Message;
            }
            else if (exception is UnauthorizedAccessException)
            {
                statusCode = StatusCodes.Status401Unauthorized;
                message = "Unauthorized access";
            }
            else if (exception is DbUpdateException)
            {
                statusCode = StatusCodes.Status400BadRequest;
                message = "Database error occurred";
            }

            context.Response.StatusCode = statusCode;

            // In production, don't expose the actual exception details
            var response = context.RequestServices.GetService<IWebHostEnvironment>().IsDevelopment()
                ? new { error = message, details = exception.ToString() }
                : new { error = message };

            await context.Response.WriteAsJsonAsync(response);
        }
    }

    public class IPTrackingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<IPTrackingMiddleware> _logger;

        public IPTrackingMiddleware(RequestDelegate next, ILogger<IPTrackingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Get IP address
            var ipAddress = GetIpAddress(context);

            // Log the IP address
            _logger.LogInformation("Request from IP: {IpAddress}", ipAddress);

            // Check if the IP is blacklisted
            if (await IsIpBlacklisted(ipAddress))
            {
                _logger.LogWarning("Blocked request from blacklisted IP: {IpAddress}", ipAddress);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Access denied" });
                return;
            }

            // Continue processing
            await _next(context);
        }

        private string GetIpAddress(HttpContext context)
        {
            if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
                return context.Request.Headers["X-Forwarded-For"];
            else
                return context.Connection.RemoteIpAddress?.MapToIPv4().ToString();
        }

        private async Task<bool> IsIpBlacklisted(string ipAddress)
        {
            // In a real implementation, check against a blacklist
            // For demonstration, we're just returning false
            return false;
        }
    }

    #endregion

    #region Authorization Handlers

    public class WhitelabelAccessRequirement : IAuthorizationRequirement
    {
    }

    public class WhitelabelAccessHandler : AuthorizationHandler<WhitelabelAccessRequirement>
    {
        private readonly ApplicationDbContext _context;

        public WhitelabelAccessHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context, 
            WhitelabelAccessRequirement requirement)
        {
            // Get user ID and claims
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return; // No user ID claim found
            }

            // Get the whitelabel ID from the context
            var whitelabelId = GetWhitelabelIdFromContext(context);
            if (string.IsNullOrEmpty(whitelabelId))
            {
                return; // No whitelabel ID in context
            }

            // Allow if user is admin
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return;
            }

            // Handle partner role
            if (context.User.IsInRole("Partner"))
            {
                // Get user's allowed whitelabels
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return; // User not found
                }

                // For partners, check if they have access to this whitelabel
                // This is a simplified example - in a real system you would have a proper relationship
                // between partners and whitelabels
                if (user.WhitelabelId == whitelabelId)
                {
                    context.Succeed(requirement);
                    return;
                }
            }

            // Handle subpartner role
            if (context.User.IsInRole("Subpartner"))
            {
                // Get user data
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return; // User not found
                }

                // For subpartners, they need to match both whitelabel ID and tracker
                var tracker = GetTrackerFromContext(context);

                if (user.WhitelabelId == whitelabelId && user.Tracker == tracker)
                {
                    context.Succeed(requirement);
                    return;
                }
            }

            // If we get here, the user does not have access
            return;
        }

        private string GetWhitelabelIdFromContext(AuthorizationHandlerContext context)
        {
            // This would extract the whitelabel ID from the current HTTP context
            // In a real implementation, this might come from the route data, query string, or resource
            
            // For demonstration purposes:
            if (context.Resource is HttpContext httpContext)
            {
                // Try to get from route data
                if (httpContext.Request.RouteValues.TryGetValue("whitelabelId", out var whitelabelId))
                {
                    return whitelabelId?.ToString();
                }

                // Try to get from query string
                if (httpContext.Request.Query.TryGetValue("whitelabelId", out var queryWhitelabelId))
                {
                    return queryWhitelabelId.ToString();
                }

                // Try to get from request body for POST/PUT requests
                if (httpContext.Request.Method == "POST" || httpContext.Request.Method == "PUT")
                {
                    // This is a simplified example - in a real system you would need to read the body
                    // without consuming it
                    // For now, we'll just return null
                }

                // Try to get from the resource being accessed
                // This would depend on your specific implementation
            }
            
            return null;
        }

        private string GetTrackerFromContext(AuthorizationHandlerContext context)
        {
            // Similar to GetWhitelabelIdFromContext but for tracker
            // For demonstration purposes:
            if (context.Resource is HttpContext httpContext)
            {
                // Try to get from query string
                if (httpContext.Request.Query.TryGetValue("tracker", out var tracker))
                {
                    return tracker.ToString();
                }
            }
            
            return null;
        }
    }

    #endregion