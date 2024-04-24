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

    public async Task InvokeAsync(HttpContext context)
    {
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