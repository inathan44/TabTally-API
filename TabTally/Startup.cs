using Microsoft.EntityFrameworkCore;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

namespace Splyt
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<TransactionService>();
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            services.AddDbContext<SplytContext>(options =>
            {
                options.UseNpgsql(Environment.GetEnvironmentVariable("DATABASE_URL"));
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