using Microsoft.EntityFrameworkCore;
using WebServiceCosmetics.Data;
using Microsoft.AspNetCore.Identity;
using WebServiceCosmetics.Controllers;
using System;
using ModelEntities.Models;
using WebServiceCosmetics.Models;
using WebServiceCosmetics.ViewModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;


var builder = WebApplication.CreateBuilder(args);
// �������������� ������������� HTTPS

// ��������� ������������ �����������
builder.Logging.ClearProviders();  // ������� ������ ����������
builder.Logging.AddConsole();      // ��������� ����� � �������
builder.Logging.AddDebug();       // ��������� ����� � ��������
// Add services to the container.
builder.Services.AddControllersWithViews();


builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectingString = builder.Configuration.GetConnectionString(name: "WebServiceDb");
    options.UseSqlServer(connectingString);
    // ��������� ���������� �����������
    options.UseLoggerFactory(LoggerFactory.Create(builder =>
    {
        builder.AddConsole(); // ����������� � �������
        builder.AddDebug();   // ����������� � �������
    }));
    options.EnableSensitiveDataLogging(); // ��������� ����������� �������������� ������ (��������, ���������� ��������)
    options.EnableDetailedErrors(); // ��������� ��������� ������
});

builder.Services.AddIdentity<WebServiceCosmetics.Models.User, IdentityRole>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Account/AccessDenied";  // ���� � �������� "������ ��������"
});

// ��������� ����������� � ���������������
builder.Services.AddSession();

builder.Services.AddControllersWithViews();
var app = builder.Build();
// ������ ���� ��� ������ ����������

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = new[] { "��������", "�������������", "��������", "��������", "���������" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSession();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Unhandled exception");
        throw;
    }
});


app.UseRouting();
app.UseAuthentication();  // ��������� ��������������

app.UseAuthorization();


app.MapControllerRoute(
    name: "default",
       pattern: "{controller=Home}/{action=Index}/{id?}"
      // pattern: "{controller=Account}/{action=Login}/{id?}",  // ��������������� �� Login, ���� ������������ �� �����������
      //    defaults: new { controller = "Account" }

    );



app.Run();
