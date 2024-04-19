using DotNetEnv;
using Splyt;

var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
Env.Load(envPath);

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseStartup<Startup>();
    });

var host = builder.Build();

host.Run();