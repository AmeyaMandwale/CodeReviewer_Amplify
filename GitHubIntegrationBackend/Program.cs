using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using DinkToPdf;               // <-- Add this
using DinkToPdf.Contracts;     // <-- Add this
using GitHubIntegrationBackend.Data;
using GitHubIntegrationBackend.Services;
using Microsoft.Extensions.FileProviders;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddHttpClient();
builder.Services.AddScoped<GitHubService>();
builder.Services.AddScoped<SonarQubeService>();
builder.Services.AddScoped<GeminiService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<PullRequestService>();
builder.Services.AddHostedService<PullRequestScheduler>();
builder.Services.AddScoped<PRReviewService>();
builder.Services.AddScoped<ContributorService>();
builder.Services.AddScoped<GitHubPRCommentService>();
builder.Services.AddScoped<GitHubCommentService>();
builder.Services.AddScoped<GitLabCommentService>();
builder.Services.AddScoped<GitHubPRFileService>();
builder.Services.AddScoped<GitLabPRFileService>();
builder.Services.AddScoped<PRFileSyncService>();
builder.Services.AddScoped<GitLabService>();
builder.Services.AddHostedService<PRFileScheduler>();
builder.Services.AddScoped<RulePackService>();
builder.Services.AddScoped<AnalysisResultService>();
builder.Services.AddScoped<PdfStorageService>();
builder.Services.AddHttpContextAccessor();

// ---------------------------------------------------------
// ⭐ ADD THIS: DinkToPdf registration
// ---------------------------------------------------------
var context = new CustomAssemblyLoadContext();
var wkLibPath = Path.Combine(builder.Environment.ContentRootPath, "Native", "libwkhtmltox.dll");
//var wkLibPath = Path.Combine(Directory.GetCurrentDirectory(), "Native", "libwkhtmltox.dll");
if (File.Exists(wkLibPath))
{
    context.LoadUnmanagedLibrary(wkLibPath);
}

builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
builder.Services.AddScoped<PdfService>();
// ---------------------------------------------------------

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
// Serve reports folder as static URL
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "Reports")
    ),
    RequestPath = "/reports"
});
// ✅ FORCE CORS FIX
app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.Headers.Add("Access-Control-Allow-Origin", "http://localhost:5173");
        context.Response.Headers.Add("Access-Control-Allow-Headers", "*");
        context.Response.Headers.Add("Access-Control-Allow-Methods", "*");
        context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
        context.Response.StatusCode = 200;
        return;
    }

    await next();
});


app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// -------------------------
// LOAD NATIVE LIBRARIES
// -------------------------
internal class CustomAssemblyLoadContext : System.Runtime.Loader.AssemblyLoadContext
{
    public IntPtr LoadUnmanagedLibrary(string absolutePath)
    {
        return LoadUnmanagedDll(absolutePath);
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        return LoadUnmanagedDllFromPath(unmanagedDllName);
    }

    protected override System.Reflection.Assembly? Load(System.Reflection.AssemblyName assemblyName) => null;
}
