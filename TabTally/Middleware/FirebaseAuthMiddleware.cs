using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using FirebaseAdmin.Auth;

public class FirebaseAuthMiddleware
{
    private readonly RequestDelegate _next;
    public FirebaseAuthMiddleware(RequestDelegate next)
    {
        _next = next;

    }

    public async Task InvokeAsync(HttpContext context, ILogger<FirebaseAuthMiddleware> logger)
    {
        // Routes that do not require authentication (Some are for testing purposes and should be revoked in prod)
        if (Environment.GetEnvironmentVariable("ENVIRONMENT") == "development")
        {
            var excludedPaths = new[] { "/api/v1/Users" };
            if (excludedPaths.Any(path => context.Request.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }
        }

        if (context.Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
        {
            var bearerToken = authorizationHeader.FirstOrDefault()?.Split(" ").Last();
            if (bearerToken != null)
            {
                try
                {
                    var auth = FirebaseAuth.DefaultInstance;
                    var token = await auth.VerifyIdTokenAsync(bearerToken);
                    context.Items.Add("FirebaseUserId", token.Uid);
                    await _next(context);
                }
                catch (FirebaseAuthException)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Unauthorized");
                    return;
                }
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Authorization header not found");
                return;
            }
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Authorization header not found");
            return;
        }
    }
}