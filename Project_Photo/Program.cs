using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project_Photo.Areas.Videos.Services;
using Project_Photo.Data;
using Project_Photo.Models;
using Project_Photo.ViewModels;
using Project_Photo.Services;
using Project_Photo.services;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;
using DotNetEnv;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);


////////////--------------------------在應用程式啟動前下載 FFmpeg---------------
var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
var ffmpegPath = Path.Combine(wwwrootPath, "FFmpeg");

// 確保目錄存在
Directory.CreateDirectory(ffmpegPath);

// 檢查是否已存在 FFmpeg
var ffmpegExe = Path.Combine(ffmpegPath, "ffmpeg.exe");
if (!File.Exists(ffmpegExe))
{
    Console.WriteLine("FFmpeg 不存在,正在下載...");
    try
    {
        await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpegPath);
        Console.WriteLine("✅ FFmpeg 下載完成!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ FFmpeg 下載失敗: {ex.Message}");
        Console.WriteLine("請手動下載 FFmpeg 並放置到 wwwroot/FFmpeg/ 目錄");
    }
}
else
{
    Console.WriteLine("✅ FFmpeg 已存在");
}

// 設定 FFmpeg 路徑
FFmpeg.SetExecutablesPath(ffmpegPath);
////////////--------------------------

builder.Services.Configure<EmailSettingsViewModel>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddScoped<IEmailService, EmailService>();

// Add services to the container.
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 載入 .env
Env.Load();

// 正確的連線字串（不要包含 Trusted_Connection）
var connectionString = $"Server={Environment.GetEnvironmentVariable("DB_SERVER")};" +
                      $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
                      $"User ID={Environment.GetEnvironmentVariable("DB_USER")};" +
                      $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD")};" +
                      $"Encrypt={Environment.GetEnvironmentVariable("DB_ENCRYPT")};" +
                      $"TrustServerCertificate={Environment.GetEnvironmentVariable("DB_TRUST_CERTIFICATE")};" +
                      $"MultipleActiveResultSets=true";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

var AAConnectionString =
    builder.Configuration.GetConnectionString("AA");
builder.Services.AddDbContext<AAContext>(options => options.UseSqlServer(AAConnectionString));

builder.Services.AddDistributedMemoryCache(); // 使用記憶體儲存 Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session 過期時間
    options.Cookie.HttpOnly = true; // 安全性設定
    options.Cookie.IsEssential = true; // GDPR 合規
});
builder.Services.AddDbContext<AAContext>(options => options.UseSqlServer(AAConnectionString));

//避免循環參考
builder.Services.AddControllersWithViews().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});


builder.Services.AddDbContext<AAContext>(options => options.UseSqlServer(AAConnectionString));
//新增Video專用的DI容器
builder.Services.AddDbContext<Project_Photo.Areas.Videos.Models.VideosDbContext>(options => options.UseSqlServer(AAConnectionString));
///---------------------
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

//---------------Video服務註冊
//固定時間刪除未完成上傳或者上傳失敗的影片背景服務
builder.Services.AddHostedService<DraftCleanupService>();
//產生channel資料的服務
builder.Services.AddScoped<IChannelService, ChannelService>();
//註冊影片刪除的服務
builder.Services.AddScoped<IVideoDeleteService, VideoDeleteService>();
// 註冊 Video Analytics Service
builder.Services.AddScoped<IVideoAnalyticsService, VideoAnalyticsService>();
//---------------------------------------------------------------------


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

//新增Area的Route
app.MapControllerRoute(
    name: "Areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
//
app.MapControllerRoute(
      name: "areas",
      pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
    );

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
