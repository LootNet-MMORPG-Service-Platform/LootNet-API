using System.Text;
using LootNet_API.Configuration;
using LootNet_API.Data;
using LootNet_API.Hubs;
using LootNet_API.Services;
using LootNet_API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Threading.RateLimiting;

namespace LootNet_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = 10_000_000;
                options.Limits.MaxRequestHeadersTotalSize = 32_768;
            });

            builder.Services.AddControllers();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("ConfiguredClients", policy =>
                {
                    var origins = builder.Configuration
                        .GetSection("Cors:AllowedOrigins")
                        .Get<string[]>() ?? Array.Empty<string>();

                    if (origins.Length > 0)
                    {
                        policy.WithOrigins(origins)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    }
                });
            });
            builder.Services.AddRateLimiter(options =>
            {
                options.AddPolicy("api", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.User.Identity?.Name
                            ?? context.Connection.RemoteIpAddress?.ToString()
                            ?? "anonymous",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 120,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }));

                options.AddPolicy("auth", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }));

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });
            var keyString = builder.Configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(keyString))
                throw new InvalidOperationException("JWT key not found. Set Jwt__Key in environment variables or user-secrets.");
            if (Encoding.UTF8.GetByteCount(keyString) < 32)
                throw new InvalidOperationException("JWT key must be at least 32 bytes long.");
            var key = Encoding.UTF8.GetBytes(keyString);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.Configure<MarketplaceEconomyOptions>(builder.Configuration.GetSection("MarketplaceEconomy"));
            builder.Services.AddScoped<IItemGenerationService, ItemGenerationService>();
            builder.Services.AddScoped<IMarketplaceService, MarketplaceService>();
            builder.Services.AddScoped<IItemNameGenerator, ItemNameGenerator>();
            builder.Services.AddScoped<IInventoryService, InventoryService>();
            builder.Services.AddScoped<IEnemyGenerationService, EnemyGenerationService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IGenerationAdminService, GenerationAdminService>();
            builder.Services.AddScoped<IEnemyGenerationAdminService, EnemyGenerationAdminService>();
            builder.Services.AddScoped<IEquipmentService, EquipmentService>();
            builder.Services.AddScoped<IProfileService, ProfileService>();
            builder.Services.AddScoped<IChatService, ChatService>();
            builder.Services.AddScoped<CombatHandsService>();
            builder.Services.AddScoped<DamageCalculator>();
            builder.Services.AddScoped<BattleService>();
            builder.Services.AddScoped<IGameRunService, GameRunService>();
            builder.Services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();
            builder.Services.AddSignalR();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "LootNet API", Version = "v1" });

                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Type in JWT (without word 'Bearer')"
                });

                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                    Array.Empty<string>()
                    }
                });
            });

            string connectionString;
            var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

            if (string.IsNullOrEmpty(databaseUrl))
            {
                connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Database connection string not found. Set DATABASE_URL or ConnectionStrings__DefaultConnection.");
            }
            else
            {
                var databaseUri = new Uri(databaseUrl);
                var userInfo = databaseUri.UserInfo.Split(':', 2);
                if (userInfo.Length != 2)
                    throw new InvalidOperationException("DATABASE_URL must contain username and password.");
                int port = databaseUri.Port == -1 ? 5432 : databaseUri.Port;

                connectionString = new Npgsql.NpgsqlConnectionStringBuilder
                {
                    Host = databaseUri.Host,
                    Port =port,
                    Username = Uri.UnescapeDataString(userInfo[0]),
                    Password = Uri.UnescapeDataString(userInfo[1]),
                    Database = Uri.UnescapeDataString(databaseUri.AbsolutePath.TrimStart('/')),
                    SslMode = SslMode.Require,
                    TrustServerCertificate = true
                }.ToString();
            }

            builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
            builder.Services.AddDbContextFactory<AppDbContext>(
                options => options.UseNpgsql(connectionString),
                ServiceLifetime.Scoped);


            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }
            if (args.Contains("--seed"))
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var generator = scope.ServiceProvider.GetRequiredService<IItemGenerationService>();

                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                DbSeeder.Seed(db, generator);

                Console.WriteLine("Database seeded successfully!");
                return;
            }

            app.UseHttpsRedirection();
            app.Use(async (context, next) =>
            {
                context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
                context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
                context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
                context.Response.Headers.TryAdd("Cache-Control", "no-store");
                await next();
            });
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors("ConfiguredClients");
            app.UseAuthentication();
            app.UseRateLimiter();
            app.UseAuthorization();

            app.MapControllers().RequireRateLimiting("api");
            app.MapHub<GameHub>("/hub");
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.Run();
        }
    }
}

