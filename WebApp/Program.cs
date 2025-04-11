using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using WebApp.Data;
using WebApp.Models;

var builder = WebApplication.CreateBuilder(args);

// Veritaban� ba�lant�s�n� ve Lazy Loading'i yap�land�r�yoruz
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .UseLazyLoadingProxies());  // Lazy loading'i entegre ettik

// Identity servisini yap�land�r�yoruz
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();





// JWT Authentication yap�land�rmas�
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Bearer";
    options.DefaultChallengeScheme = "Bearer";
})
.AddJwtBearer("Bearer", options =>
{
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var secretKey = jwtSettings["SecretKey"];
    var key = Encoding.ASCII.GetBytes(secretKey!);

    options.RequireHttpsMetadata = false; // Geli�tirme ortam�nda HTTP'yi kabul edebiliriz
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});






// Uygulaman�n g�venlik ayarlar�n� yap�land�r�yoruz (CORS vb.)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Swagger/OpenAPI deste�i ekliyoruz
builder.Services.AddEndpointsApiExplorer(); // Swagger'� etkinle�tirir
builder.Services.AddSwaggerGen(); // Swagger UI'yi ba�latmak i�in gereklidir

// API servislerini ekliyoruz
builder.Services.AddControllers();



builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        Description = "JWT Authorization header using the Bearer scheme.",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
});


var app = builder.Build();



using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    await SeedData.SeedRolesAndUsers(userManager, roleManager);
}

// Veritaban� g�ncellemeleri i�in Migration uygulamas�
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    await SeedData.SeedRolesAndUsers(userManager, roleManager); // Role ve kullan�c�lar� seed et
}





// HTTP pipeline'�n� yap�land�r�yoruz
if (app.Environment.IsDevelopment())
{
    // Swagger UI'yi geli�tirme ortam�nda aktif ediyoruz
    app.UseSwagger(); // Swagger JSON endpoint'ini olu�turur
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApp v1");
        options.RoutePrefix = string.Empty; // Swagger UI'yi k�k dizinde ba�latmak i�in
    });
}

app.UseHttpsRedirection();

// CORS politikas�n� etkinle�tiriyoruz
app.UseCors("AllowAll");

// Kimlik do�rulama ve yetkilendirmeyi etkinle�tiriyoruz
app.UseAuthentication();
app.UseAuthorization();

// API controller'lar�n� y�nlendiriyoruz
app.MapControllers();
app.Run();
