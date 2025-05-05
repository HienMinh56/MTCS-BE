using DotNetEnv;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MTCS.APIService.Middlewares;
using MTCS.Data;
using MTCS.Data.DTOs;
using MTCS.Data.Helpers;
using MTCS.Service;
using MTCS.Service.Handler;
using MTCS.Service.Hubs;
using MTCS.Service.Interfaces;
using MTCS.Service.Services;
using OfficeOpenXml;
using StackExchange.Redis;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

Env.Load();
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
// Add services to the container.
builder.Services.AddScoped<UnitOfWork>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDriverService, DriverService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<ITractorService, TractorService>();
builder.Services.AddScoped<ITrailerService, TrailerService>();
builder.Services.AddScoped<IIncidentReportsService, IncidentReportsService>();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<IFuelReportService, FuelReportService>();
builder.Services.AddScoped<IDeliveryReportService, DeliveryReportService>();
builder.Services.AddScoped<IDeliveryStatusService, DeliveryStatusService>();
builder.Services.AddScoped<IPriceTableService, PriceTableService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IAdminService, AdminService>();


builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IFirebaseStorageService, FirebaseStorageService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddSingleton<IFCMService, FCMService>();
builder.Services.AddScoped<ISystemConfigurationServices, SystemConfigurationServices>();
builder.Services.AddScoped<IDriverDailyWorkingTimeService, DriverDailyWorkingTimeService>();
builder.Services.AddScoped<IDriverWeeklySummaryService, DriverWeeklySummaryService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddSingleton<WebSocketHandler>();
builder.Services.AddHostedService<VehicleRegistrationService>();
builder.Services.AddHostedService<VehicleMaintenanceService>();
builder.Services.AddSignalR();


builder.Services.AddSingleton(opt => StorageClient.Create(GoogleCredential.FromFile("..\\..\\nomnomfood-3f50b-firebase-adminsdk-pc2ef-9697ade1d4.json")));
builder.Services.AddSingleton(opt => StorageClient.Create(GoogleCredential.FromFile("..\\..\\driverapp-3845f-firebase-adminsdk-fbsvc-19a996d823.json")));

var googleCredentialJson = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS");
GoogleCredential credential;

if (!string.IsNullOrEmpty(googleCredentialJson))
{
    credential = GoogleCredential.FromJson(googleCredentialJson);
}
else
{
    credential = GoogleCredential.FromFile("..\\..\\driverapp-3845f-firebase-adminsdk-fbsvc-19a996d823.json");
}

var firestoreClient = new FirestoreClientBuilder { Credential = credential }.Build();
var firestoreDb = FirestoreDb.Create("driverapp-3845f", firestoreClient);
builder.Services.AddSingleton(firestoreDb);

//var googleCredentialJson = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS");
//GoogleCredential credential = GoogleCredential.FromJson(googleCredentialJson);


////var credential = GoogleCredential.FromFile("..\\..\\driverapp-3845f-firebase-adminsdk-fbsvc-19a996d823.json");
//var firestoreClient = new FirestoreClientBuilder { Credential = credential }.Build();
//var firestoreDb = FirestoreDb.Create("driverapp-3845f", firestoreClient);
//builder.Services.AddSingleton(firestoreDb);





var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JWTSettings>(jwtSettings);
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        RoleClaimType = ClaimTypes.Role
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception is SecurityTokenExpiredException)
            {
                context.Response.Headers.Append("Token-Expired", "true");
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole("Admin", "Staff", "Driver")
        .Build();

    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Staff", policy => policy.RequireRole("Staff"));
    options.AddPolicy("Driver", policy => policy.RequireRole("Driver"));
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidateModelAttribute>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.MaxDepth = 64;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MTCS-Server", Version = "v1" });

    c.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary" // Định dạng cho file upload
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT token obtained from the login endpoint",
        Name = "Authorization"
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
            new string[] {}
        }
    });
});

//logging azure
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);


builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>();

    options.AddPolicy("AllowSpecificOrigin",
        builder => builder
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetConnectionString("Redis");
    return ConnectionMultiplexer.Connect(configuration);
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowSpecificOrigin");
app.MapHub<LocationHub>("/locationHub");
//app.MapHub<ChatHub>("/chatHub");
app.UseWebSockets();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var socket = await context.WebSockets.AcceptWebSocketAsync();
            await WebSocketHandler.Handle(context, socket);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }
    else
    {
        await next();
    }
});

app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthentication();

app.UseAuthorization();
app.UseMiddleware<ExceptionHandlerMiddleware>();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
