using Application.Interfaces;
using Application.Service.BackService;
using Application.Service.User;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using MediatR;
using Application.Handlers.NasaHandler;
using MongoDB.Driver;
using Application.Service.CrowdService;
using Application.Service.Integration;
using Application.Settings;

var builder = WebApplication.CreateBuilder(args);

// ������ ������ ����������� �� ������������
var mongoSettings = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
var mongoClient = new MongoClient(mongoSettings.ConnectionString);
var database = mongoClient.GetDatabase(mongoSettings.DatabaseName);

// ������������ IMongoDatabase
builder.Services.AddSingleton<IMongoDatabase>(database);

// ��������� MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
    typeof(GetFiresByDateCommand).Assembly,
    typeof(GetFiresByDateHandler).Assembly
));

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// ����������� ��������
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHttpClient<NasaFirmsService>();
builder.Services.AddSingleton<FireDataCleanupService>();
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddHostedService<CoordinationService>();
builder.Services.AddScoped<IPhotoVerificationService, PhotoVerificationService>();
builder.Services.AddScoped<ICrowdService, CrowdService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// ��������� JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FireComponent", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontendAndLocalhost", policy =>
    {
        policy.WithOrigins(
                "https://firenotification-frondend.onrender.com",
                "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontendAndLocalhost");
// �����: ������� middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
