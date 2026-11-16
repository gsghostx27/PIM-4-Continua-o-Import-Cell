// Inicialização da API ImportCell: banco de dados, autenticação, serviços e rotas.

using System;
using System.Text;
using ImportCell.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);


// ==========================================
// BANCO DE DADOS - SQLITE PORTÁTIL
// ==========================================

var databasePath = System.IO.Path.Combine(
    AppContext.BaseDirectory,
    "importcell.db"
);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite($"Data Source={databasePath}");
});


// ==========================================
// AUTENTICAÇÃO JWT
// ==========================================

var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Jwt:Key não configurada."
    );
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)
                ),

                ValidateIssuer = false,
                ValidateAudience = false,

                ValidateLifetime = true,

                ClockSkew = TimeSpan.Zero
            };
    });

builder.Services.AddAuthorization();

// ==========================================
// CORS
// ==========================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// ==========================================
// SERVIÇOS
// ==========================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// ==========================================
// BANCO DE DADOS
// ==========================================

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    context.Database.EnsureCreated();
}

// ==========================================
// PIPELINE
// ==========================================

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ==========================================
// INICIALIZAÇÃO
// ==========================================

app.Run();
