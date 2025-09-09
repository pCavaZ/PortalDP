using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PortalDP.Application.Interfaces;
using PortalDP.Application.Mapping;
using PortalDP.Application.Services;
using PortalDP.Infrastructure.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// NUEVO: Configurar puerto para Render
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Configurar Serilog - MODIFICADO para producción
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    // COMENTADO: En Render no se pueden escribir archivos locales
    // .WriteTo.File("logs/academia-costura-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Personalizar respuestas de validación
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .SelectMany(x => x.Value.Errors)
                .Select(x => x.ErrorMessage)
                .ToList();

            var result = new
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors
            };

            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(result);
        };
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Configuración de Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Academia de Costura API",
        Version = "v1",
        Description = "API para la gestión de clases de la Academia de Costura",
        Contact = new OpenApiContact
        {
            Name = "Academia de Costura",
            Email = "info@academiacostura.com"
        }
    });

    // Configuración de autenticación JWT en Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme. 
                        Enter 'Bearer' [space] and then your token in the text input below.
                        Example: 'Bearer 12345abcdef'",
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
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });

    // Incluir comentarios XML si están disponibles
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// MODIFICADO: Configuración de base de datos para Render
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") ??
                      builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string not found. Set DATABASE_URL environment variable or DefaultConnection in appsettings.json");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    });

    // Solo en desarrollo
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Configuración de JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY");

if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("JWT SecretKey not found in configuration or JWT_SECRET_KEY environment variable.");
}

var key = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"] ?? Environment.GetEnvironmentVariable("JWT_ISSUER"),
            ValidAudience = jwtSettings["Audience"] ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.FromMinutes(5) // Tolerancia de 5 minutos
        };

        // Eventos para logging
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Log.Warning("JWT Authentication failed: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Log.Debug("JWT Token validated for user: {User}", context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StudentOnly", policy => policy.RequireRole("Student"));
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Servicios de aplicación
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();

// MODIFICADO: CORS para Render
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        // Obtener orígenes permitidos de variables de entorno o configuración
        var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")?.Split(',') ??
                           builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ??
                           new[] { "http://localhost:3000", "http://localhost:5173" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database")
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

// Configuración de caché (opcional)
builder.Services.AddMemoryCache();

// Rate limiting (opcional)
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("AuthPolicy", fixedOptions =>
    {
        fixedOptions.PermitLimit = 10; // 10 intentos
        fixedOptions.Window = TimeSpan.FromMinutes(1); // por minuto
        fixedOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        fixedOptions.QueueLimit = 5;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Academia de Costura API v1");
        c.RoutePrefix = string.Empty; // Swagger UI en la raíz
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        c.DefaultModelsExpandDepth(-1); // No expandir modelos por defecto
    });

    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    // COMENTADO: HSTS puede causar problemas en algunos entornos de producción
    // app.UseHsts();
}

// Middleware personalizado para logging de requests
app.UseMiddleware<RequestLoggingMiddleware>();

// COMENTADO: Render maneja HTTPS automáticamente
// app.UseHttpsRedirection();

app.UseCors("AllowReactApp");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Health checks endpoint
app.MapHealthChecks("/health");

app.MapControllers();

// MODIFICADO: Manejo más robusto de migraciones
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // En producción, solo aplicar migraciones si es necesario
        if (app.Environment.IsProduction())
        {
            // Verificar si hay migraciones pendientes
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                Log.Information("Applying {Count} pending migrations", pendingMigrations.Count());
                await context.Database.MigrateAsync();
                Log.Information("Migrations applied successfully");
            }
            else
            {
                Log.Information("No pending migrations to apply");
            }
        }
        else
        {
            // En desarrollo, crear la base si no existe
            await context.Database.EnsureCreatedAsync();
        }

        Log.Information("Database connection successful");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Failed to connect to database or apply migrations during startup");

        // En lugar de fallar completamente, intentar continuar sin DB
        if (app.Environment.IsProduction())
        {
            Log.Warning("Continuing without database connection - some features may not work");
        }
        else
        {
            throw; // En desarrollo, sí fallar para debuggear
        }
    }
}

Log.Information("Academia de Costura API started successfully");

app.Run();

// Middleware personalizado para logging de requests
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;

        // Log request
        _logger.LogInformation(
            "Incoming {Method} request to {Path} from {IPAddress}",
            context.Request.Method,
            context.Request.Path,
            context.Connection.RemoteIpAddress);

        try
        {
            await _next(context);
        }
        finally
        {
            var elapsed = DateTime.UtcNow - startTime;

            // Log response
            _logger.LogInformation(
                "Completed {Method} {Path} with status {StatusCode} in {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                elapsed.TotalMilliseconds);
        }
    }
}




// VERSION 3



//using System.Text;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.AspNetCore.RateLimiting;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.IdentityModel.Tokens;
//using Microsoft.OpenApi.Models;
//using PortalDP.Application.Interfaces;
//using PortalDP.Application.Mapping;
//using PortalDP.Application.Services;
//using PortalDP.Infrastructure.Data;
//using Serilog;

//var builder = WebApplication.CreateBuilder(args);

//// NUEVO: Configurar puerto para Render
//var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
//builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

//// Configurar Serilog - MODIFICADO para producción
//Log.Logger = new LoggerConfiguration()
//    .ReadFrom.Configuration(builder.Configuration)
//    .Enrich.FromLogContext()
//    .WriteTo.Console()
//    // COMENTADO: En Render no se pueden escribir archivos locales
//    // .WriteTo.File("logs/academia-costura-.txt", rollingInterval: RollingInterval.Day)
//    .CreateLogger();

//builder.Host.UseSerilog();

//// Add services to the container
//builder.Services.AddControllers()
//    .ConfigureApiBehaviorOptions(options =>
//    {
//        // Personalizar respuestas de validación
//        options.InvalidModelStateResponseFactory = context =>
//        {
//            var errors = context.ModelState
//                .SelectMany(x => x.Value.Errors)
//                .Select(x => x.ErrorMessage)
//                .ToList();

//            var result = new
//            {
//                Success = false,
//                Message = "Validation failed",
//                Errors = errors
//            };

//            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(result);
//        };
//    });

//// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();

//// Configuración de Swagger
//builder.Services.AddSwaggerGen(c =>
//{
//    c.SwaggerDoc("v1", new OpenApiInfo
//    {
//        Title = "Academia de Costura API",
//        Version = "v1",
//        Description = "API para la gestión de clases de la Academia de Costura",
//        Contact = new OpenApiContact
//        {
//            Name = "Academia de Costura",
//            Email = "info@academiacostura.com"
//        }
//    });

//    // Configuración de autenticación JWT en Swagger
//    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//    {
//        Description = @"JWT Authorization header using the Bearer scheme. 
//                        Enter 'Bearer' [space] and then your token in the text input below.
//                        Example: 'Bearer 12345abcdef'",
//        Name = "Authorization",
//        In = ParameterLocation.Header,
//        Type = SecuritySchemeType.ApiKey,
//        Scheme = "Bearer"
//    });

//    c.AddSecurityRequirement(new OpenApiSecurityRequirement
//    {
//        {
//            new OpenApiSecurityScheme
//            {
//                Reference = new OpenApiReference
//                {
//                    Type = ReferenceType.SecurityScheme,
//                    Id = "Bearer"
//                },
//                Scheme = "oauth2",
//                Name = "Bearer",
//                In = ParameterLocation.Header,
//            },
//            new List<string>()
//        }
//    });

//    // Incluir comentarios XML si están disponibles
//    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
//    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
//    if (File.Exists(xmlPath))
//    {
//        c.IncludeXmlComments(xmlPath);
//    }
//});

//// MODIFICADO: Configuración de base de datos para Render
//var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") ??
//                      builder.Configuration.GetConnectionString("DefaultConnection");

//if (string.IsNullOrEmpty(connectionString))
//{
//    throw new InvalidOperationException("Connection string not found. Set DATABASE_URL environment variable or DefaultConnection in appsettings.json");
//}

//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//{
//    options.UseNpgsql(connectionString, npgsqlOptions =>
//    {
//        npgsqlOptions.EnableRetryOnFailure(
//            maxRetryCount: 3,
//            maxRetryDelay: TimeSpan.FromSeconds(5),
//            errorCodesToAdd: null);
//    });

//    // Solo en desarrollo
//    if (builder.Environment.IsDevelopment())
//    {
//        options.EnableSensitiveDataLogging();
//        options.EnableDetailedErrors();
//    }
//});

//// Configuración de JWT Authentication
//var jwtSettings = builder.Configuration.GetSection("JwtSettings");
//var secretKey = jwtSettings["SecretKey"] ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY");

//if (string.IsNullOrEmpty(secretKey))
//{
//    throw new InvalidOperationException("JWT SecretKey not found in configuration or JWT_SECRET_KEY environment variable.");
//}

//var key = Encoding.UTF8.GetBytes(secretKey);

//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
//        options.SaveToken = true;
//        options.TokenValidationParameters = new TokenValidationParameters
//        {
//            ValidateIssuer = true,
//            ValidateAudience = true,
//            ValidateLifetime = true,
//            ValidateIssuerSigningKey = true,
//            ValidIssuer = jwtSettings["Issuer"] ?? Environment.GetEnvironmentVariable("JWT_ISSUER"),
//            ValidAudience = jwtSettings["Audience"] ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
//            IssuerSigningKey = new SymmetricSecurityKey(key),
//            ClockSkew = TimeSpan.FromMinutes(5) // Tolerancia de 5 minutos
//        };

//        // Eventos para logging
//        options.Events = new JwtBearerEvents
//        {
//            OnAuthenticationFailed = context =>
//            {
//                Log.Warning("JWT Authentication failed: {Error}", context.Exception.Message);
//                return Task.CompletedTask;
//            },
//            OnTokenValidated = context =>
//            {
//                Log.Debug("JWT Token validated for user: {User}", context.Principal?.Identity?.Name);
//                return Task.CompletedTask;
//            }
//        };
//    });

//builder.Services.AddAuthorization(options =>
//{
//    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
//    options.AddPolicy("StudentOnly", policy => policy.RequireRole("Student"));
//});

//// AutoMapper
//builder.Services.AddAutoMapper(typeof(MappingProfile));

//// Servicios de aplicación
//builder.Services.AddScoped<IStudentService, StudentService>();
//builder.Services.AddScoped<ICalendarService, CalendarService>();

//// MODIFICADO: CORS para Render
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowReactApp", policy =>
//    {
//        // Obtener orígenes permitidos de variables de entorno o configuración
//        var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")?.Split(',') ??
//                           builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ??
//                           new[] { "http://localhost:3000", "http://localhost:5173" };

//        policy.WithOrigins(allowedOrigins)
//              .AllowAnyMethod()
//              .AllowAnyHeader()
//              .AllowCredentials();
//    });
//});

//// Health checks
//builder.Services.AddHealthChecks()
//    .AddDbContextCheck<ApplicationDbContext>("database")
//    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

//// Configuración de caché (opcional)
//builder.Services.AddMemoryCache();

//// Rate limiting (opcional)
//builder.Services.AddRateLimiter(options =>
//{
//    options.AddFixedWindowLimiter("AuthPolicy", fixedOptions =>
//    {
//        fixedOptions.PermitLimit = 10; // 10 intentos
//        fixedOptions.Window = TimeSpan.FromMinutes(1); // por minuto
//        fixedOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
//        fixedOptions.QueueLimit = 5;
//    });
//});

//var app = builder.Build();

//// Configure the HTTP request pipeline
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI(c =>
//    {
//        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Academia de Costura API v1");
//        c.RoutePrefix = string.Empty; // Swagger UI en la raíz
//        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
//        c.DefaultModelsExpandDepth(-1); // No expandir modelos por defecto
//    });

//    app.UseDeveloperExceptionPage();
//}
//else
//{
//    app.UseExceptionHandler("/Error");
//    // COMENTADO: HSTS puede causar problemas en algunos entornos de producción
//    // app.UseHsts();
//}

//// Middleware personalizado para logging de requests
//app.UseMiddleware<RequestLoggingMiddleware>();

//// COMENTADO: Render maneja HTTPS automáticamente
//// app.UseHttpsRedirection();

//app.UseCors("AllowReactApp");

//app.UseRateLimiter();

//app.UseAuthentication();
//app.UseAuthorization();

//// Health checks endpoint
//app.MapHealthChecks("/health");

//app.MapControllers();

//// MODIFICADO: Aplicar migraciones en producción también
//using (var scope = app.Services.CreateScope())
//{
//    try
//    {
//        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

//        // Aplicar migraciones en todos los entornos
//        await context.Database.MigrateAsync();

//        Log.Information("Database connection successful and migrations applied");
//    }
//    catch (Exception ex)
//    {
//        Log.Fatal(ex, "Failed to connect to database or apply migrations during startup");
//        throw;
//    }
//}

//Log.Information("Academia de Costura API started successfully");

//app.Run();

//// Middleware personalizado para logging de requests
//public class RequestLoggingMiddleware
//{
//    private readonly RequestDelegate _next;
//    private readonly ILogger<RequestLoggingMiddleware> _logger;

//    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
//    {
//        _next = next;
//        _logger = logger;
//    }

//    public async Task InvokeAsync(HttpContext context)
//    {
//        var startTime = DateTime.UtcNow;

//        // Log request
//        _logger.LogInformation(
//            "Incoming {Method} request to {Path} from {IPAddress}",
//            context.Request.Method,
//            context.Request.Path,
//            context.Connection.RemoteIpAddress);

//        try
//        {
//            await _next(context);
//        }
//        finally
//        {
//            var elapsed = DateTime.UtcNow - startTime;

//            // Log response
//            _logger.LogInformation(
//                "Completed {Method} {Path} with status {StatusCode} in {ElapsedMs}ms",
//                context.Request.Method,
//                context.Request.Path,
//                context.Response.StatusCode,
//                elapsed.TotalMilliseconds);
//        }
//    }
//}




//VERSION 2



//using System.Text;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.AspNetCore.RateLimiting;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.IdentityModel.Tokens;
//using Microsoft.OpenApi.Models;
//using PortalDP.Application.Interfaces;
//using PortalDP.Application.Mapping;
//using PortalDP.Application.Services;
//using PortalDP.Infrastructure.Data;
//using Serilog;

//var builder = WebApplication.CreateBuilder(args);

//// Configurar Serilog
//Log.Logger = new LoggerConfiguration()
//    .ReadFrom.Configuration(builder.Configuration)
//    .Enrich.FromLogContext()
//    .WriteTo.Console()
//    .WriteTo.File("logs/academia-costura-.txt", rollingInterval: RollingInterval.Day)
//    .CreateLogger();

//builder.Host.UseSerilog();

//// Add services to the container
//builder.Services.AddControllers()
//    .ConfigureApiBehaviorOptions(options =>
//    {
//// Personalizar respuestas de validación
//options.InvalidModelStateResponseFactory = context =>
//    {
//    var errors = context.ModelState
//        .SelectMany(x => x.Value.Errors)
//        .Select(x => x.ErrorMessage)
//        .ToList();

//    var result = new
//        {
//        Success = false,
//        Message = "Validation failed",
//        Errors = errors
//        };

//    return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(result);
//    };
//    });

//// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();

//// Configuración de Swagger
//builder.Services.AddSwaggerGen(c =>
//{
//c.SwaggerDoc("v1", new OpenApiInfo
//{
//Title = "Academia de Costura API",
//Version = "v1",
//Description = "API para la gestión de clases de la Academia de Costura",
//Contact = new OpenApiContact
//{
//Name = "Academia de Costura",
//Email = "info@academiacostura.com"
//}
//});

//// Configuración de autenticación JWT en Swagger
//c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//{
//Description = @"JWT Authorization header using the Bearer scheme. 
//                        Enter 'Bearer' [space] and then your token in the text input below.
//                        Example: 'Bearer 12345abcdef'",
//Name = "Authorization",
//In = ParameterLocation.Header,
//Type = SecuritySchemeType.ApiKey,
//Scheme = "Bearer"
//});

//c.AddSecurityRequirement(new OpenApiSecurityRequirement
//    {
//        {
//            new OpenApiSecurityScheme
//            {
//                Reference = new OpenApiReference
//                {
//                    Type = ReferenceType.SecurityScheme,
//                    Id = "Bearer"
//                },
//                Scheme = "oauth2",
//                Name = "Bearer",
//                In = ParameterLocation.Header,
//            },
//            new List<string>()
//        }
//    });

//// Incluir comentarios XML si están disponibles
//var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
//var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
//if (File.Exists(xmlPath))
//{
//c.IncludeXmlComments(xmlPath);
//}
//});

//// Configuración de base de datos
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//if (string.IsNullOrEmpty(connectionString))
//{
//throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
//}

//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//{
//options.UseNpgsql(connectionString, npgsqlOptions =>
//{
//npgsqlOptions.EnableRetryOnFailure(
//    maxRetryCount: 3,
//    maxRetryDelay: TimeSpan.FromSeconds(5),
//    errorCodesToAdd: null);
//});

//// Solo en desarrollo
//if (builder.Environment.IsDevelopment())
//{
//options.EnableSensitiveDataLogging();
//options.EnableDetailedErrors();
//}
//});

//// Configuración de JWT Authentication
//var jwtSettings = builder.Configuration.GetSection("JwtSettings");
//var secretKey = jwtSettings["SecretKey"];

//if (string.IsNullOrEmpty(secretKey))
//{
//throw new InvalidOperationException("JWT SecretKey not found in configuration.");
//}

//var key = Encoding.UTF8.GetBytes(secretKey);

//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
//options.SaveToken = true;
//options.TokenValidationParameters = new TokenValidationParameters
//{
//ValidateIssuer = true,
//ValidateAudience = true,
//ValidateLifetime = true,
//ValidateIssuerSigningKey = true,
//ValidIssuer = jwtSettings["Issuer"],
//ValidAudience = jwtSettings["Audience"],
//IssuerSigningKey = new SymmetricSecurityKey(key),
//ClockSkew = TimeSpan.FromMinutes(5) // Tolerancia de 5 minutos
//};

//// Eventos para logging
//options.Events = new JwtBearerEvents
//{
//OnAuthenticationFailed = context =>
//{
//Log.Warning("JWT Authentication failed: {Error}", context.Exception.Message);
//return Task.CompletedTask;
//},
//OnTokenValidated = context =>
//{
//Log.Debug("JWT Token validated for user: {User}", context.Principal?.Identity?.Name);
//return Task.CompletedTask;
//}
//};
//});

//builder.Services.AddAuthorization(options =>
//{
//options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
//options.AddPolicy("StudentOnly", policy => policy.RequireRole("Student"));
//});

//// AutoMapper
//builder.Services.AddAutoMapper(typeof(MappingProfile));

//// Servicios de aplicación
//builder.Services.AddScoped<IStudentService, StudentService>();
//builder.Services.AddScoped<ICalendarService, CalendarService>();

//// CORS
//builder.Services.AddCors(options =>
//{
//options.AddPolicy("AllowReactApp", policy =>
//{
//policy.WithOrigins(
//        builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ??
//        new[] { "http://localhost:3000", "http://localhost:5173" }) // React dev servers
//    .AllowAnyMethod()
//    .AllowAnyHeader()
//    .AllowCredentials();
//});
//});

//// Health checks
//builder.Services.AddHealthChecks()
//    .AddDbContextCheck<ApplicationDbContext>("database")
//    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

//// Configuración de caché (opcional)
//builder.Services.AddMemoryCache();

//// Rate limiting (opcional)
//builder.Services.AddRateLimiter(options =>
//{
//options.AddFixedWindowLimiter("AuthPolicy", fixedOptions =>
//{
//fixedOptions.PermitLimit = 10; // 10 intentos
//fixedOptions.Window = TimeSpan.FromMinutes(1); // por minuto
//fixedOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
//fixedOptions.QueueLimit = 5;
//});
//});

//var app = builder.Build();

//// Configure the HTTP request pipeline
//if (app.Environment.IsDevelopment())
//{
//app.UseSwagger();
//app.UseSwaggerUI(c =>
//{
//c.SwaggerEndpoint("/swagger/v1/swagger.json", "Academia de Costura API v1");
//c.RoutePrefix = string.Empty; // Swagger UI en la raíz
//c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
//c.DefaultModelsExpandDepth(-1); // No expandir modelos por defecto
//});

//app.UseDeveloperExceptionPage();
//}
//else
//{
//app.UseExceptionHandler("/Error");
//app.UseHsts();
//}

//// Middleware personalizado para logging de requests
//app.UseMiddleware<RequestLoggingMiddleware>();

//app.UseHttpsRedirection();

//app.UseCors("AllowReactApp");

//app.UseRateLimiter();

//app.UseAuthentication();
//app.UseAuthorization();

//// Health checks endpoint
//app.MapHealthChecks("/health");

//app.MapControllers();

//// Asegurar que la base de datos existe y aplicar migraciones
//using (var scope = app.Services.CreateScope())
//{
//try
//{
//var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

//if (app.Environment.IsDevelopment())
//{
//// En desarrollo, aplicar migraciones automáticamente
//await context.Database.EnsureCreatedAsync();
//}

//Log.Information("Database connection successful");
//}
//catch (Exception ex)
//{
//Log.Fatal(ex, "Failed to connect to database during startup");
//throw;
//}
//}

//Log.Information("Academia de Costura API started successfully");

//app.Run();

//// Middleware personalizado para logging de requests
//public class RequestLoggingMiddleware
//{
//    private readonly RequestDelegate _next;
//    private readonly ILogger<RequestLoggingMiddleware> _logger;

//    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
//    {
//        _next = next;
//        _logger = logger;
//    }

//    public async Task InvokeAsync(HttpContext context)
//    {
//        var startTime = DateTime.UtcNow;

//        // Log request
//        _logger.LogInformation(
//            "Incoming {Method} request to {Path} from {IPAddress}",
//            context.Request.Method,
//            context.Request.Path,
//            context.Connection.RemoteIpAddress);

//        try
//        {
//            await _next(context);
//        }
//        finally
//        {
//            var elapsed = DateTime.UtcNow - startTime;

//            // Log response
//            _logger.LogInformation(
//                "Completed {Method} {Path} with status {StatusCode} in {ElapsedMs}ms",
//                context.Request.Method,
//                context.Request.Path,
//                context.Response.StatusCode,
//                elapsed.TotalMilliseconds);
//        }
//    }
//}





// VERSION 1

// API/Program.cs - Configuración para PostgreSQL local

//using System.Text;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.IdentityModel.Tokens;
//using PortalDP.Domain.Entities;
//using PortalDP.Infrastructure.Data;

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container
//builder.Services.AddControllers();
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(c =>
//{
//    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
//    {
//        Title = "Academia de Costura API",
//        Version = "v1",
//        Description = "API para gestión de la Academia de Costura"
//    });
//});

//// Configuración de base de datos PostgreSQL
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

//if (string.IsNullOrEmpty(connectionString))
//{
//    throw new InvalidOperationException("Connection string 'DefaultConnection' not found. Please check your appsettings.json");
//}

//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//{
//    options.UseNpgsql(connectionString, npgsqlOptions =>
//    {
//        npgsqlOptions.EnableRetryOnFailure(
//            maxRetryCount: 3,
//            maxRetryDelay: TimeSpan.FromSeconds(5),
//            errorCodesToAdd: null);
//    });

//    // Solo en desarrollo - mostrar SQL generado
//    if (builder.Environment.IsDevelopment())
//    {
//        options.EnableSensitiveDataLogging();
//        options.EnableDetailedErrors();
//    }
//});

//// JWT Authentication
//var jwtSettings = builder.Configuration.GetSection("JwtSettings");
//var secretKey = jwtSettings["SecretKey"];

//if (!string.IsNullOrEmpty(secretKey))
//{
//    var key = Encoding.UTF8.GetBytes(secretKey);

//    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//        .AddJwtBearer(options =>
//        {
//            options.RequireHttpsMetadata = false; // Solo para desarrollo local
//            options.SaveToken = true;
//            options.TokenValidationParameters = new TokenValidationParameters
//            {
//                ValidateIssuer = true,
//                ValidateAudience = true,
//                ValidateLifetime = true,
//                ValidateIssuerSigningKey = true,
//                ValidIssuer = jwtSettings["Issuer"],
//                ValidAudience = jwtSettings["Audience"],
//                IssuerSigningKey = new SymmetricSecurityKey(key),
//                ClockSkew = TimeSpan.FromMinutes(5)
//            };
//        });
//}

//builder.Services.AddAuthorization();

//// CORS para desarrollo
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowReactApp", policy =>
//    {
//        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
//                           ?? new[] { "http://localhost:3000", "http://localhost:5173" };

//        policy.WithOrigins(allowedOrigins)
//              .AllowAnyMethod()
//              .AllowAnyHeader()
//              .AllowCredentials();
//    });
//});

//// Health checks
//builder.Services.AddHealthChecks()
//    .AddDbContextCheck<ApplicationDbContext>("database");

//var app = builder.Build();

//// Configure the HTTP request pipeline
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI(c =>
//    {
//        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Academia de Costura API v1");
//        c.RoutePrefix = string.Empty; // Swagger UI en la raíz
//    });
//    app.UseDeveloperExceptionPage();
//}

//app.UseHttpsRedirection();
//app.UseCors("AllowReactApp");

//if (!string.IsNullOrEmpty(secretKey))
//{
//    app.UseAuthentication();
//}

//app.UseAuthorization();
//app.MapControllers();
//app.MapHealthChecks("/health");

//// Inicializar base de datos
//await InitializeDatabaseAsync(app);

//Console.WriteLine("🚀 Academia de Costura API started successfully");
//Console.WriteLine($"🌐 Environment: {app.Environment.EnvironmentName}");
//Console.WriteLine($"📊 Swagger UI: {(app.Environment.IsDevelopment() ? "https://localhost:7xxx" : "Disabled in production")}");

//app.Run();

//// Método para inicializar la base de datos
//static async Task InitializeDatabaseAsync(WebApplication app)
//{
//    using var scope = app.Services.CreateScope();
//    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

//    try
//    {
//        logger.LogInformation("🔍 Checking database connection...");

//        // Verificar conexión
//        var canConnect = await context.Database.CanConnectAsync();

//        if (canConnect)
//        {
//            logger.LogInformation("✅ Database connection successful");

//            // Crear tablas si no existen
//            var created = await context.Database.EnsureCreatedAsync();
//            if (created)
//            {
//                logger.LogInformation("✅ Database and tables created successfully");
//            }
//            else
//            {
//                logger.LogInformation("ℹ️ Database already exists");
//            }

//            // Seed data si está vacía
//            if (!await context.Students.AnyAsync())
//            {
//                await SeedInitialDataAsync(context, logger);
//            }
//        }
//        else
//        {
//            logger.LogError("❌ Cannot connect to database. Please check:");
//            logger.LogError("   - PostgreSQL is running");
//            logger.LogError("   - Connection string is correct");
//            logger.LogError("   - Database 'AcademiaCostura' exists");
//        }
//    }
//    catch (Exception ex)
//    {
//        logger.LogError(ex, "❌ Database initialization failed");
//        logger.LogError("Connection string: {ConnectionString}",
//            context.Database.GetConnectionString()?.Replace("Password=", "Password=***"));
//    }
//}

//// Datos iniciales
//static async Task SeedInitialDataAsync(ApplicationDbContext context, ILogger logger)
//{
//    logger.LogInformation("🌱 Seeding initial data...");

//    var students = new[]
//    {
//        new Student
//        {
//            Name = "María García López",
//            DNI = "12345678A",
//            Email = "maria.garcia@email.com",
//            Phone = "666123456",
//            IsActive = true,
//            CreatedAt = DateTime.UtcNow,
//            UpdatedAt = DateTime.UtcNow
//        },
//        new Student
//        {
//            Name = "Ana López Martín",
//            DNI = "87654321B",
//            Email = "ana.lopez@email.com",
//            Phone = "666654321",
//            IsActive = true,
//            CreatedAt = DateTime.UtcNow,
//            UpdatedAt = DateTime.UtcNow
//        },
//        new Student
//        {
//            Name = "Carmen Ruiz Sánchez",
//            DNI = "11223344C",
//            Email = "carmen.ruiz@email.com",
//            Phone = "666112233",
//            IsActive = true,
//            CreatedAt = DateTime.UtcNow,
//            UpdatedAt = DateTime.UtcNow
//        },
//        new Student
//        {
//            Name = "Marta Sánchez Rodríguez",
//            DNI = "55667788D",
//            Email = "marta.sanchez@email.com",
//            Phone = "666556677",
//            IsActive = true,
//            CreatedAt = DateTime.UtcNow,
//            UpdatedAt = DateTime.UtcNow
//        },
//        new Student
//        {
//            Name = "Rosa Martín González",
//            DNI = "99887766E",
//            Email = "rosa.martin@email.com",
//            Phone = "666998877",
//            IsActive = true,
//            CreatedAt = DateTime.UtcNow,
//            UpdatedAt = DateTime.UtcNow
//        }
//    };

//    context.Students.AddRange(students);
//    await context.SaveChangesAsync();

//    logger.LogInformation("✅ Seeded {Count} students", students.Length);
//}