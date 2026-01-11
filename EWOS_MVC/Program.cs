using EWOS_MVC.Services;
using EWOS_MVC.Models;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Connect DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnection")));

// Config Windows authentication
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();
// Bind EmailSettings dari appsettings.json
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Daftarkan EmailService sebagai scope
builder.Services.AddScoped<EmailService>(sp =>
{
    var emailSettings = sp.GetRequiredService<IOptions<EmailSettings>>().Value;
    var emailContextHelper = sp.GetRequiredService<EmailContextHelper>();
    var adUserService = sp.GetRequiredService<AdUserService>();

    return new EmailService(emailSettings, emailContextHelper, adUserService);
});
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});
builder.Services.AddScoped<AdUserService>();
builder.Services.AddScoped<WeekHelper>();
builder.Services.AddScoped<YearsHelper>();
builder.Services.AddScoped<CalculateSavingHelper>();
builder.Services.AddScoped<DashboardService>();

builder.Services.AddScoped<EmailContextHelper>();
// Tambahkan memory cache 
builder.Services.AddMemoryCache();

//set value desimal jadi titik
var cultureInfo = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Storage", "Uploads");

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    ServeUnknownFileTypes = true
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<WindowsAuthMiddleware>();
app.UseAuthorization();


app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

//  Preload EF Core model di awal
//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//    Console.WriteLine("Warming up EF Core...");
//    var _ = db.Users.FirstOrDefault();
//    Console.WriteLine(" EF Core model ready");
//}

app.Run();
