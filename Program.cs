using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartStudentManagement.Models;
using SmartStudentManagement.Data;
using SmartStudentManagement.Services;
using Serilog;


namespace SmartStudentManagement
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog((context, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14, shared: true));

            // Control dotnet restore

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(
                        new JsonStringEnumConverter());
                });

            builder.Services.AddEndpointsApiExplorer();

            // Swagger + JWT Bearer authentication
            builder.Services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer",
                    new Microsoft.OpenApi.OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        In = Microsoft.OpenApi.ParameterLocation.Header,
                        Description = "Enter your JWT access token."
                    });

                c.AddSecurityRequirement(document =>
                    new Microsoft.OpenApi.OpenApiSecurityRequirement
                    {
                        [new Microsoft.OpenApi.OpenApiSecuritySchemeReference(
                            "Bearer", document)] = new List<string>()
                    });
            });

            // Database
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString(
                        "DefaultConnection")));

            // ASP.NET Core Identity
            builder.Services.AddIdentity<User, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // JWT configuration
            var jwtKey = builder.Configuration["Jwt:SecurityKey"];
            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new InvalidOperationException(
                    "JWT signing key is not configured. Set Jwt__SecurityKey using User Secrets or an environment variable.");
            if (string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Issuer"]) ||
                string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Audience"]))
                throw new InvalidOperationException("JWT issuer and audience must be configured.");

            if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
                throw new InvalidOperationException(
                    "Database connection string is not configured. Set ConnectionStrings__DefaultConnection using User Secrets or an environment variable.");

            if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
            {
                throw new InvalidOperationException(
                    "JWT signing key must be at least 32 bytes.");
            }

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],

                        ValidateAudience = true,
                        ValidAudience = builder.Configuration["Jwt:Audience"],

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtKey)),

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30),

                        RoleClaimType =
                            System.Security.Claims.ClaimTypes.Role,

                        NameClaimType =
                            System.Security.Claims.ClaimTypes.NameIdentifier
                    };

                // Validate the account and its current role on each authenticated request.
                // This makes account deactivation and role changes take effect immediately,
                // at the cost of a user/role lookup per authenticated request.
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var userId = context.Principal?.FindFirst(
                            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        if (string.IsNullOrWhiteSpace(userId))
                        {
                            context.Fail("Token does not contain a user identifier.");
                            return;
                        }

                        var userManager = context.HttpContext.RequestServices
                            .GetRequiredService<UserManager<User>>();
                        var user = await userManager.FindByIdAsync(userId);
                        if (user is null || !user.IsActive)
                        {
                            context.Fail("The account is unavailable or inactive.");
                            return;
                        }

                        var currentRoles = await userManager.GetRolesAsync(user);
                        var tokenRoles = context.Principal!.FindAll(
                                System.Security.Claims.ClaimTypes.Role)
                            .Select(claim => claim.Value);
                        if (!tokenRoles.Any(role => currentRoles.Contains(
                                role, StringComparer.OrdinalIgnoreCase)))
                            context.Fail("The account role has changed. Please sign in again.");
                    },
                    OnForbidden = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";

                        var roles = context.HttpContext
                            .GetEndpoint()?
                            .Metadata
                            .GetOrderedMetadata<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
                            .SelectMany(a => (a.Roles ?? "").Split(
                                ',',
                                StringSplitOptions.RemoveEmptyEntries |
                                StringSplitOptions.TrimEntries))
                            .Distinct()
                            .ToArray() ?? [];

                        var message = roles.Length > 0
                            ? $"Access denied. This endpoint requires the following role(s): {string.Join(", ", roles)}."
                            : "Access denied. You don't have permission to access this resource.";

                        return context.Response.WriteAsJsonAsync(new
                        {
                            status = 403,
                            message
                        });
                    }
                };
            });

            builder.Services.AddAuthorization();

            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IAttendanceService, AttendanceService>();
            builder.Services.AddScoped<ICourseService, CourseService>();
            builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
            builder.Services.AddScoped<IGradeService, GradeService>();
            builder.Services.AddScoped<IStudentService, StudentService>();

            // Build application
            var app = builder.Build();

            // Request logging captures method, path, status code and elapsed time.
            app.UseSerilogRequestLogging();

            // Create roles if they don't exist.
            using (var scope = app.Services.CreateScope())
            {
                await DbSeeder.SeedRolesAsync(scope.ServiceProvider);
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            try
            {
                await app.RunAsync();
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
