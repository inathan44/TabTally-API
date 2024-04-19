using Microsoft.EntityFrameworkCore;

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

            app.UseEndpoints(endpoints =>
            {

                endpoints.MapControllers();
                endpoints.MapGet("/api/v1", () => "The API is running!");
            });
        }
    }
}