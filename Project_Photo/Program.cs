using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project_Photo.Areas.Videos.Services;
using Project_Photo.Data;
using Project_Photo.Models;
using Project_Photo.services;
using Project_Photo.Services;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;



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

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

var AAConnectionString =
    builder.Configuration.GetConnectionString("AA");
builder.Services.AddDbContext<AaContext>(options => options.UseSqlServer(AAConnectionString));
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
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

//新增Area的Route
app.MapControllerRoute(
    name: "Areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
//
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
