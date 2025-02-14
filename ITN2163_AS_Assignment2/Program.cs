using ITN2163_AS_Assignment2.Model;
using ITN2163_AS_Assignment2.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddDbContext<AuthDbContext>();
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Configure Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.AllowedForNewUsers = true;  // Allow lockout for new users
})
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(Config =>
{
    Config.LoginPath = "/Login";
    Config.ExpireTimeSpan = TimeSpan.FromDays(7);
    Config.SlidingExpiration = true;
    // Set the cookie to persist even after the browser is closed.
    Config.Cookie.IsEssential = true;
    Config.Cookie.HttpOnly = true;
    Config.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Register RsaKeyManager with IConfiguration
builder.Services.AddSingleton<RsaKeyManager>();

// Register MyEncryptionService
builder.Services.AddSingleton<MyEncryptionService>();

builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddDistributedMemoryCache(); // Required for session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true; // Make sure the session cookie is HttpOnly for security (Prevents JavaScript access (Mitigates XSS attacks)
    options.Cookie.IsEssential = true; // Mark session cookie as essential for the application
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Enforce HTTPS (Ensures session works only on HTTPS)
    options.Cookie.SameSite = SameSiteMode.Strict; // Prevent CSRF attacks
});

builder.Services.AddScoped<IAuditLogService, AuditLogService>();

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

builder.Services.AddScoped<PasswordHistoryService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// For status code errors (e.g., 404, 403), re-execute the pipeline with the error code appended to the URL.
app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.UseSession();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
