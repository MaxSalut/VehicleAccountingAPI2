using Microsoft.EntityFrameworkCore;
using System.Text.Json; // Не використовується безпосередньо тут, але може бути потрібним для конфігурації JsonSerializerOptions
using System.Text.Json.Serialization;
using VehicleAccountingAPI.Data; // Ваш namespace для DbContext

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles; // Ігноруємо цикли
    });

// Додаємо CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder => // Назвіть політику відповідно, наприклад, "CorsPolicy"
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Реєстрація DbContext
builder.Services.AddDbContext<VehicleAccountingContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Показує деталі помилок у режимі розробки
}
else
{
    app.UseExceptionHandler("/error"); // Додайте обробник помилок для production
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Для статичних файлів, якщо є (наприклад, UI)
app.UseRouting();

// Використовуємо CORS
app.UseCors("AllowAll"); // Використовуйте ту саму назву політики, що й при додаванні

app.UseAuthorization(); // Якщо у вас є авторизація

app.MapControllers();

app.Run();