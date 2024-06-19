using API.Configuration;
using API.Services;
using API;
using Application.Data;
using Application.SeedWork;
using Application.Services.Catalog;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IO;
using System.Text;

namespace API
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            ApplicationSettings appSetting = new();
            Configuration.GetSection("AppSettings").Bind(appSetting);
            services.AddSingleton<IApplicationSettings>(appSetting);
            Context.ConnectionString = Configuration.GetConnectionString("Database");
            InitilizeDatabase();

            var AutoRetryHangfire = new AutomaticRetryAttribute { Attempts = 3, DelaysInSeconds = new int[3] { 10, 30, 90 } };
            services.AddSingleton(AutoRetryHangfire);
            GlobalJobFilters.Filters.Add(AutoRetryHangfire);
            services.AddHangfire((provider, configuration) => configuration
            .UseInMemoryStorage()
            .UseFilter(provider.GetRequiredService<AutomaticRetryAttribute>())
            );
            services.AddHangfireServer();
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSetting.SigningKey)),
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    //مشخص می کند که اگر توکن منقض شده چقدر دیگر اعتبار داشته باشد که پیش فرض آن 5 دقیقه است
                    //یعنی اگر توکن منقضی شده باشد باز هم بهش 5 دقیق وقت داده می شود
                    ClockSkew = TimeSpan.Zero
                };
            });
            services.AddAuthorization();

            services.AddGrpc(Option =>
            {
                Option.EnableDetailedErrors = true;
                Option.MaxSendMessageSize = 20971520;
                Option.MaxReceiveMessageSize = 8388608;
            });
            services.AddCors(o => o.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
            }));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider srp)
        {
            ApplicationSettings appSetting = (ApplicationSettings)srp.GetService<IApplicationSettings>();
            appSetting.WwwRootPath = env.WebRootPath;

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            //app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseGrpcWeb(); // Must be added between UseRouting and UseEndpoints
            app.UseCors();

            app.UseHangfireDashboard();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<GreeterService>().EnableGrpcWeb().RequireCors("AllowAll");
                endpoints.MapGrpcService<CategoryService>().EnableGrpcWeb().RequireCors("AllowAll");
                endpoints.MapGrpcService<ProductService>().EnableGrpcWeb().RequireCors("AllowAll");
                endpoints.MapGrpcService<EmailService>().EnableGrpcWeb().RequireCors("AllowAll");
                endpoints.MapGrpcService<AccountService>().EnableGrpcWeb().RequireCors("AllowAll");
                endpoints.MapGrpcService<ImportDataService>().EnableGrpcWeb().RequireCors("AllowAll");
                endpoints.MapGrpcService<FilterService>().EnableGrpcWeb().RequireCors("AllowAll");
                endpoints.MapGrpcService<GroupService>().EnableGrpcWeb().RequireCors("AllowAll");
                endpoints.MapGrpcService<CatalogService>().EnableGrpcWeb().RequireCors("AllowAll");
                endpoints.MapGrpcService<OrderService>().EnableGrpcWeb().RequireCors("AllowAll");
                endpoints.MapGrpcService<PoService>().EnableGrpcWeb().RequireCors("AllowAll");
                endpoints.MapGrpcService<TutorialService>().EnableGrpcWeb().RequireCors("AllowAll");
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
                endpoints.MapHangfireDashboard();
            });
        }
        private void InitilizeDatabase()
        {
            ///using var Db = new Context();
            //Db.Database.Migrate();
        }
    }
}