using Microsoft.EntityFrameworkCore;
using WebServiceCosmetics.Data;
using Microsoft.AspNetCore.Identity;
using WebServiceCosmetics.Controllers;


var builder = WebApplication.CreateBuilder(args);
// Принудительное использование HTTPS

// Добавляем конфигурацию логирования
builder.Logging.ClearProviders();  // Очищаем старые провайдеры
builder.Logging.AddConsole();      // Добавляем вывод в консоль
builder.Logging.AddDebug();       // Добавляем вывод в отладчик
// Add services to the container.
builder.Services.AddControllersWithViews();


builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectingString = builder.Configuration.GetConnectionString(name: "WebServiceDb");
    options.UseSqlServer(connectingString);
    // Включение подробного логирования
    options.UseLoggerFactory(LoggerFactory.Create(builder =>
    {
        builder.AddConsole(); // Логирование в консоль
        builder.AddDebug();   // Логирование в отладку
    }));
    options.EnableSensitiveDataLogging(); // Включение логирования чувствительных данных (например, параметров запросов)
    options.EnableDetailedErrors(); // Включение подробных ошибок
});


// Добавляем контроллеры с представлениями
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
app.UseAuthentication();  // Добавляем аутентификацию

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
       pattern: "{controller=Employer}/{action=Index}/{id?}"
      // pattern: "{controller=Account}/{action=Login}/{id?}",  // Перенаправление на Login, если пользователь не авторизован
      //    defaults: new { controller = "Account" }

    );


app.Run();
