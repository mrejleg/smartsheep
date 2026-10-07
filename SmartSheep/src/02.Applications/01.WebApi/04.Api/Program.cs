using Api;
using Api.ApplicationCore;
using Api.ApplicationCore.Interfaces.Auths;
using Api.Domain.Models;
using Api.Infrastructure.Data.DbContexts;
using Api.Services;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Project.Core.Exceptions;
using Project.Core.Interfaces.Logging;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

#region ReadAppsettingsJson
Action<AppSetting> appSettings = opt =>
{
    opt.DatabaseProvider = builder.Configuration["Database:Provider"];
    opt.DatabaseNamingConvention = builder.Configuration["Database:NamingConvention"];
    opt.ConnectionStringsDefaultConnection = builder.Configuration["ConnectionStrings:DefaultConnection"];
    opt.ApplicationUrlPublish = builder.Configuration["Application:UrlPublish"];
    opt.ApplicationApiBaseUrl = builder.Configuration["Application:ApiBaseUrl"];
    opt.ApplicationUrlWebApp = builder.Configuration["Application:UrlWebApp"];
    opt.ApplicationMaximumLoginAttempt = builder.Configuration["Application:MaximumLoginAttempt"];
    opt.ApplicationPageLength = builder.Configuration["Application:PageLength"];
    opt.JwtKey = builder.Configuration["Jwt:Key"];
    opt.JwtIssuer = builder.Configuration["Jwt:Issuer"];
    opt.JwtExpiresInDays = builder.Configuration["Jwt:ExpiresInDays"];
    opt.SecurityEncryptKeyBase64 = builder.Configuration["Security:EncryptKeyBase64"];
    opt.SecurityEncryptKeyMD5 = builder.Configuration["Security:EncryptKeyMD5"];
    opt.FileUrl = builder.Configuration["File:Url"];
    opt.FileMaximumFileSize = builder.Configuration["File:MaximumFileSize"];
    opt.FileAllowedFileExtensions = builder.Configuration["File:AllowedFileExtensions"];
    opt.FirebaseProjectId = builder.Configuration["Firebase:ProjectId"];
    opt.FirebaseType = builder.Configuration["Firebase:Credential:Type"];
    opt.FirebasePrivateKeyId = builder.Configuration["Firebase:Credential:PrivateKeyId"];
    opt.FirebasePrivateKey = builder.Configuration["Firebase:Credential:PrivateKey"];
    opt.FirebaseClientEmail = builder.Configuration["Firebase:Credential:ClientEmail"];
    opt.FirebaseClientId = builder.Configuration["Firebase:Credential:ClientId"];
    opt.FirebaseAuthUri = builder.Configuration["Firebase:Credential:AuthUri"];
    opt.FirebaseTokenUri = builder.Configuration["Firebase:Credential:TokenUri"];
    opt.FirebaseAuthProviderX509CertUrl = builder.Configuration["Firebase:Credential:AuthProviderX509CertUrl"];
    opt.FirebaseClientX509CertUrl = builder.Configuration["Firebase:Credential:ClientX509CertUrl"];
    opt.FirebaseUniverseDomain = builder.Configuration["Firebase:Credential:UniverseDomain"];
    opt.YoloConfidenceThreshold = builder.Configuration["Yolo:ConfidenceThreshold"];
    opt.YoloMinimumSucklingSeconds = builder.Configuration["Yolo:MinimumSucklingSeconds"];
    opt.YoloSyncIdleSeconds = builder.Configuration["Yolo:SyncIdleSeconds"];
    opt.YoloPendingFolder = builder.Configuration["Yolo:PendingFolder"];
    opt.YoloMaximumProcessFps = builder.Configuration["Yolo:MaximumProcessFps"];
    opt.YoloInferenceThreads = builder.Configuration["Yolo:InferenceThreads"];
    opt.YoloSourceRefreshSeconds = builder.Configuration["Yolo:SourceRefreshSeconds"];
    opt.YoloSourceCodes = builder.Configuration["Yolo:SourceCodes"];
    opt.YoloPostureTolerance = builder.Configuration["Yolo:PostureTolerance"];
    opt.YoloShrinkThreshold = builder.Configuration["Yolo:ShrinkThreshold"];
    opt.YoloMissingToleranceSeconds = builder.Configuration["Yolo:MissingToleranceSeconds"];
    opt.YoloUdderZoneMargin = builder.Configuration["Yolo:UdderZoneMargin"];
    opt.YoloEweDirectionWindowSeconds = builder.Configuration["Yolo:EweDirectionWindowSeconds"];
    opt.YoloEweMinimumMove = builder.Configuration["Yolo:EweMinimumMove"];
    opt.YoloLambMovingWindowSeconds = builder.Configuration["Yolo:LambMovingWindowSeconds"];
    opt.YoloLambMinimumSpeed = builder.Configuration["Yolo:LambMinimumSpeed"];
    opt.YoloBaselineWeight = builder.Configuration["Yolo:BaselineWeight"];
    opt.YoloBaselineResetSeconds = builder.Configuration["Yolo:BaselineResetSeconds"];
    opt.YoloUdderZoneHysteresis = builder.Configuration["Yolo:UdderZoneHysteresis"];
    opt.YoloThresholdHysteresis = builder.Configuration["Yolo:ThresholdHysteresis"];
    opt.YoloTrackConfirmFrames = builder.Configuration["Yolo:TrackConfirmFrames"];
    opt.YoloNmsIouThreshold = builder.Configuration["Yolo:NmsIouThreshold"];
    opt.YoloNmsContainmentThreshold = builder.Configuration["Yolo:NmsContainmentThreshold"];
    opt.YoloLambMinimumAreaRatio = builder.Configuration["Yolo:LambMinimumAreaRatio"];
    opt.YoloLambMaximumAreaRatio = builder.Configuration["Yolo:LambMaximumAreaRatio"];
    opt.YoloLambPartAreaRatio = builder.Configuration["Yolo:LambPartAreaRatio"];
    opt.YoloApiUrl = builder.Configuration["Yolo:ApiUrl"];
    opt.YoloClientId = builder.Configuration["Yolo:ClientId"];
    opt.YoloClientSecret = builder.Configuration["Yolo:ClientSecret"];

};

builder.Services.Configure(appSettings);
builder.Services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<AppSetting>>().Value);
#endregion

#region SetupDatabase
if (string.Equals(
    builder.Configuration["Database:NamingConvention"],
    "snakecase",
    StringComparison.OrdinalIgnoreCase))
{
    DefaultTypeMap.MatchNamesWithUnderscores = true;
}

if (string.Equals(
    builder.Configuration["Database:Provider"],
    "sqlserver",
    StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlServer(
            builder.Configuration["ConnectionStrings:DefaultConnection"],
            sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null));
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    });
}
#endregion

#region Jwts
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = false,
        RequireExpirationTime = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

if (string.Equals(builder.Configuration["Database:NamingConvention"], "pascalcase", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddMvc()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
        });
}
#endregion

builder.Services.AddCors(option =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    option.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod();

        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins);
        }
        else if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("https://localhost:44325", "http://localhost:44325");
        }
    });
});

// Respect ASPNETCORE_URLS / --urls when supplied (local tests, containers, and
// parallel instances). Fall back to the application default otherwise.
if (string.IsNullOrWhiteSpace(builder.Configuration["urls"]) &&
    !string.IsNullOrWhiteSpace(builder.Configuration["Application:UrlPublish"]))
{
    builder.WebHost.UseUrls(builder.Configuration["Application:UrlPublish"]);
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<FirebasePushService>();
builder.Services.AddHostedService<ReminderSchedulerService>();
builder.Services.AddHostedService<SourceVideoHealthCheckService>();
builder.Services.AddMemoryCache();
builder.Services.AddApplicationCores();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

var app = builder.Build();

#region InitializeDatabase
await using (var scope = app.Services.CreateAsyncScope())
{
    var services = scope.ServiceProvider;

    var applicationLoger = services.GetRequiredService<IAppLogger<ApplicationDbContext>>();
    try
    {
        var applicationContext = services.GetRequiredService<ApplicationDbContext>();
        applicationContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
        var shouldInitializeOnStartup = builder.Configuration.GetValue<bool>("Database:InitializeOnStartup", true);
        var canConnect = await applicationContext.Database.CanConnectAsync();
        var pendingMigrations = canConnect
            ? await applicationContext.Database.GetPendingMigrationsAsync()
            : [];

        if (shouldInitializeOnStartup || !canConnect || pendingMigrations.Any())
        {
            await applicationContext.Database.MigrateAsync();
            await ApplicationDbContextSeed.ApplicationDbContextSeedAsync(applicationContext, builder.Configuration["Security:EncryptKeyMD5"], builder.Configuration["Application:ClientId"], applicationLoger);
            await ApplicationDbContextView.ApplicationDbContextViewAsync(applicationContext, applicationLoger);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Migration error:");
        Console.WriteLine(ex.ToString());
        applicationLoger.LogError(ex.Message.ToString());
    }
}
#endregion

await using (var scope = app.Services.CreateAsyncScope())
{
    var authService = scope.ServiceProvider.GetRequiredService<IAuthApiService>();
    await authService.WarmUpScopeAuthorizationAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Scalar:Enabled"))
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = builder.Configuration["Scalar:Name"];
        options.DarkMode = false;
        options.DefaultHttpClient = new KeyValuePair<ScalarTarget, ScalarClient>(ScalarTarget.CSharp, ScalarClient.RestSharp);
        options.HideModels = false;
        options.Layout = ScalarLayout.Modern;
        options.ShowSidebar = true;

        options.Authentication = new ScalarAuthenticationOptions
        {
            PreferredSecuritySchemes = ["Bearer"]
        };
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.UseMiddleware<ErrorHandlerMiddleware>();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Auth}/{action=Index}/{id?}");

app.MapControllers();
app.Run();
