using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using WebApp.Data;
using WebApp.Models;

var builder = WebApplication.CreateBuilder(args);

// Veritabanı bağlantısını ve Lazy Loading'i yapılandırıyoruz
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .UseLazyLoadingProxies());  // Lazy loading'i entegre ettik

// Identity servisini yapılandırıyoruz
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();





// JWT Authentication yapılandırması
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

    options.RequireHttpsMetadata = false; // Geliştirme ortamında HTTP'yi kabul edebiliriz
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






// Uygulamanın güvenlik ayarlarını yapılandırıyoruz (CORS vb.)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Swagger/OpenAPI desteği ekliyoruz
builder.Services.AddEndpointsApiExplorer(); // Swagger'ı etkinleştirir
builder.Services.AddSwaggerGen(); // Swagger UI'yi başlatmak için gereklidir

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

// Veritabanı güncellemeleri için Migration uygulaması
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    await SeedData.SeedRolesAndUsers(userManager, roleManager); // Role ve kullanıcıları seed et
}





// HTTP pipeline'ını yapılandırıyoruz
if (app.Environment.IsDevelopment())
{
    // Swagger UI'yi geliştirme ortamında aktif ediyoruz
    app.UseSwagger(); // Swagger JSON endpoint'ini oluşturur
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApp v1");
        options.RoutePrefix = string.Empty; // Swagger UI'yi kök dizinde başlatmak için
    });
}

app.UseHttpsRedirection();

// CORS politikasını etkinleştiriyoruz
app.UseCors("AllowAll");

// Kimlik doğrulama ve yetkilendirmeyi etkinleştiriyoruz
app.UseAuthentication();
app.UseAuthorization();

// API controller'larını yönlendiriyoruz
app.MapControllers();
app.Run();
