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
            var excludedPaths = new[] { "/api/v1/Users", "/api/v1/Groups", "/api/v1/Groups/members", "/api/v1/Transactions/details", "/api/v1/Transactions" };
            if (excludedPaths.Any(path => context.Request.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
            {
                logger.LogInformation("Path does not require authentication: {path}", context.Request.Path);
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
                    await context.Response.WriteAsync("No valid token provided");
                    return;
                }
            }
            else
            {
                logger.LogError("Bearer token not found");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Authorization header not found (1)");
                return;
            }
        }
        else
        {
            logger.LogError("Authorization header not found");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Authorization header not found (2)");
            return;
        }
    }
}