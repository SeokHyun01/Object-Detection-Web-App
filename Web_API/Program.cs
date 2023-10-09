using Business.Repository.IRepository;
using Business.Repository;
using DataAccess.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Web_API.Helper;
using DataAccess;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo { Title = "Web API", Version = "v1.0.0" });
	c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		In = ParameterLocation.Header,
		Description = "Enter bearer and then token.",
		Name = "Authorization",
		Type = SecuritySchemeType.ApiKey
	});
	c.AddSecurityRequirement(new OpenApiSecurityRequirement {
				   {
					 new OpenApiSecurityScheme
					 {
					   Reference = new OpenApiReference
					   {
						 Type = ReferenceType.SecurityScheme,
						 Id = "Bearer"
					   }
					  },
					  new string[] { }
					}
				});
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
builder.Services.AddIdentity<AppUser, IdentityRole>()
	.AddDefaultTokenProviders()
	.AddEntityFrameworkStores<AppDbContext>();

var apiSettingsSection = builder.Configuration.GetSection("APISettings");
builder.Services.Configure<APISettings>(apiSettingsSection);
var apiSettings = apiSettingsSection.Get<APISettings>();
var key = Encoding.ASCII.GetBytes(apiSettings.SecretKey);
builder.Services.AddAuthentication(opt =>
{
	opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
	opt.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
	x.RequireHttpsMetadata = false;
	x.SaveToken = true;
	x.TokenValidationParameters = new()
	{
		ValidateIssuerSigningKey = true,
		IssuerSigningKey = new SymmetricSecurityKey(key),
		ValidateAudience = true,
		ValidateIssuer = true,
		ValidAudience = apiSettings.ValidAudience,
		ValidIssuer = apiSettings.ValidIssuer,
		ClockSkew = TimeSpan.Zero
	};
});

builder.Services.AddScoped<IFCMInfoRepository, FCMInfoRepository>();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddCors(o => o.AddPolicy("Development", builder =>
{
	builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
}));

builder.WebHost.UseUrls("http://*:8094;https://*:8095");

builder.Services.AddResponseCompression(options =>
{
	options.EnableForHttps = true;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseResponseCompression();

app.UseCors("Development");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
