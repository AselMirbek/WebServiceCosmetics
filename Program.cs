using Microsoft.EntityFrameworkCore;
using WebServiceCosmetics.Data;
using Microsoft.AspNetCore.Identity;
using WebServiceCosmetics.Controllers;


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


// ��������� ����������� � ���������������
builder.Services.AddControllersWithViews();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

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
       pattern: "{controller=Employer}/{action=Index}/{id?}"
      // pattern: "{controller=Account}/{action=Login}/{id?}",  // ��������������� �� Login, ���� ������������ �� �����������
      //    defaults: new { controller = "Account" }

    );


app.Run();
