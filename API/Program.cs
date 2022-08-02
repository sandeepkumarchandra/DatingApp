using System.Text;
using API.Data;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using API.Middleware;
using API.Services;
using API.SignalR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.IdentityModel.Tokens;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
        builder.Services.AddScoped<ITokenService, TokenService>();
        builder.Services.AddScoped<IPhotoService, PhotoService>();
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
        builder.Services.AddScoped<LogUserActivity>();
        builder.Services.AddAutoMapper(typeof(AutoMapperProfiles).Assembly);
        builder.Services.AddSingleton<PresenceTracker>();

        // builder.Services.AddCors(options =>
        // {
        //     options.AddPolicy("MyMyAllowCredentialsPolicy",
        //         policy =>
        //         {
        //             policy.WithOrigins("https://localhost:4200")
        //                 .AllowCredentials();
        //         });
        // });
        // Add services to the container.
        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        //builder.Services.AddDbContext<DataContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectionString")));
        builder.Services.AddDbContext<DataContext>(options =>
        {
            options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnectionString"));
        });
        builder.Services.AddCors();
        
        builder.Services.AddIdentityCore<AppUser>(opt => {
            opt.Password.RequireNonAlphanumeric = false;
        })
            .AddRoles<AppRole>()
            .AddRoleManager<RoleManager<AppRole>>()
            .AddSignInManager<SignInManager<AppUser>>()
            .AddRoleValidator<RoleValidator<AppRole>>()
            .AddEntityFrameworkStores<DataContext>();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options => {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey=true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["TokenKey"])),
                ValidateIssuer=false,
                ValidateAudience=false
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if(!string.IsNullOrEmpty(accessToken) &&
                        path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                }
            };
        });

        builder.Services.AddAuthorization(opt =>{
            opt.AddPolicy("RequiredAdminRole", policy => policy.RequireRole("Admin"));
            opt.AddPolicy("ModeratePhotoRole", Policy => Policy.RequireRole("Admin","Moderator"));
        });

        builder.Services.AddSignalR();
 
        var app = builder.Build();


        
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        //for seeding data into database
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        try{
            var conext = services.GetRequiredService<DataContext>();
            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
            await conext.Database.MigrateAsync();
            await Seed.SeedUsers(userManager, roleManager);
        }
        catch(Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex,"An error occoured during migraiton");
        }

        app.UseMiddleware<ExceptionMiddleware>();

        app.UseHttpsRedirection();
        
        app.UseRouting();

        //app.UseCors("MyMyAllowCredentialsPolicy");
        app.UseCors(x => x.AllowAnyHeader().AllowAnyMethod().AllowCredentials().WithOrigins("https://localhost:4200","http://localhost:4200"));
        
        app.UseAuthentication();

        app.UseAuthorization();

        app.MapControllers();

        app.MapHub<PresenceHub>("hubs/presence");
        app.MapHub<MessageHub>("hubs/message");

        await app.RunAsync();
    }
}