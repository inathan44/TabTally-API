using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

namespace Splyt
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<TransactionService>();
            services
                .AddControllers()
                .AddNewtonsoftJson(opts =>
                {
                    opts.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                    opts.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                });

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            services.AddLogging();
            services.AddDbContext<SplytContext>(options =>
            {
                options.UseNpgsql(Environment.GetEnvironmentVariable("DATABASE_URL"));
            });

            services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigins",
                    builder =>
                    {
                        builder.WithOrigins("http://localhost:3000")
                               .AllowAnyHeader()
                               .AllowAnyMethod();
                    });
            });

            string? pathToFirebaseAdminSDKJson = Environment.GetEnvironmentVariable("PATH_TO_FIREBASE_JSON");
            if (pathToFirebaseAdminSDKJson == null)
            {
                throw new Exception("PATH_TO_FIREBASE_JSON environment variable not set");
            }
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile(pathToFirebaseAdminSDKJson)
            });
        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseCors("AllowSpecificOrigins");

            app.UseAuthorization();

            app.UseMiddleware<FirebaseAuthMiddleware>();

            app.UseEndpoints(endpoints =>
            {

                endpoints.MapControllers();
                endpoints.MapGet("/api/v1", () => "The API is running!");
            });
        }
    }
}