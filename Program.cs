using Microsoft.EntityFrameworkCore;
using MedVaultAPI.Data;
using MedVaultAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// CORS — allow Ionic frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowIonic", policy =>
    {
        policy.AllowAnyOrigin()   
              .AllowAnyHeader()
              .AllowAnyMethod();
});
});

// MSSQL via EF Core
builder.Services.AddDbContext<MedVaultDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Service
builder.Services.AddSingleton<JwtService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient<AIQueryService>();
var app = builder.Build();

// Auto-create DB tables on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MedVaultDbContext>();
    db.Database.EnsureCreated();
}

// ✅ Correct middleware order
app.UseRouting();           // 1. Routing first
app.UseCors("AllowIonic");  // 2. CORS second
app.UseAuthorization();     // 3. Auth third


app.MapControllers();       // 4. Controllers last

app.Run();