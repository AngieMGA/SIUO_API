using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SIUO_API.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// CORS
// ==========================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// ==========================================
// CONTROLADORES
// ==========================================

builder.Services.AddControllers();

// ==========================================
// SERVICIOS
// ==========================================

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();

builder.Services.AddScoped<SqlConnectionFactory>();
builder.Services.AddScoped<IDispositivoRepository, DispositivoRepository>();

// ==========================================
// JWT
// ==========================================

var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "No se encontró la clave Jwt:Key."
    );
}

builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme
)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,

        IssuerSigningKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            ),

        ValidateIssuer = true,

        ValidIssuer =
            builder.Configuration["Jwt:Issuer"],

        ValidateAudience = true,

        ValidAudience =
            builder.Configuration["Jwt:Audience"],

        ValidateLifetime = true,

        ClockSkew = TimeSpan.Zero
    };
});

// ==========================================
// SWAGGER
// ==========================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ==========================================
// CONSTRUIR APLICACIÓN
// ==========================================

var app = builder.Build();

// ==========================================
// SWAGGER
// ==========================================

app.UseSwagger();
app.UseSwaggerUI();

// ==========================================
// CORS
// ==========================================

app.UseCors("ReactPolicy");

// ==========================================
// HTTPS
// ==========================================

//app.UseHttpsRedirection();

// ==========================================
// AUTENTICACIÓN
// ==========================================

app.UseAuthentication();

app.UseAuthorization();

// ==========================================
// CONTROLLERS
// ==========================================

app.MapControllers();

app.Run();