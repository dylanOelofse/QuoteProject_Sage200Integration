using QuotesProject.Interfaces;
using QuotesProject.Services;
using QuotesProject.Data;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();                       // the store where session data is kept in memory

builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".QuotesProject.Session";                 // recognisable in dev-tools
    options.Cookie.HttpOnly = true;                                 // no JS access (defualt))
    options.Cookie.IsEssential = true;                              // required for session to work without consent
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;        // HTTPS Only
    options.Cookie.SameSite = SameSiteMode.Lax;                     // mitigates CSRF attacks (default)
    options.IdleTimeout = TimeSpan.FromMinutes(30);             
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";                                // redirect to login page if not authenticated
        options.LogoutPath = "/Logout";                              // redirect to logout page
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);          // session timeout
        options.SlidingExpiration = true;                            // reset timeout on activity

        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = ctx =>
            {
                if (!ctx.Request.Headers.Accept.ToString().Contains("text/html"))
                    ctx.Response.StatusCode = 401;
                else
                    ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = ctx =>
            {
                if (!ctx.Request.Headers.Accept.ToString().Contains("text/html"))
                    ctx.Response.StatusCode = 403;
                else
                    ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");

// Initialize the database engine
DatabaseEngine.Initialize(builder.Configuration);

// Register services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<IQuoteLineService, QuoteLineService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Quote/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();  // builds HttpContext.User from the auth cookie
app.UseSession();           // makes HttpContext.Session available for use in controllers and views
app.UseAuthorization();

app.MapStaticAssets();

app.MapGet("/", context =>
{
    context.Response.Redirect("/Login");
    return Task.CompletedTask;
});

app.MapControllers();

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Quote}/{action=Index}/{id?}")
//    .WithStaticAssets();
// Not using conventional routing , using attribute routing instead. LaunchSetings specifies launch URL

app.Run();
