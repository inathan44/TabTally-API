using Microsoft.EntityFrameworkCore;

public class FindOrCreateUserMiddleware
{
    private readonly RequestDelegate _next;

    public FindOrCreateUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, ILogger<FindOrCreateUserMiddleware> logger)
    {
        var dbContext = httpContext.RequestServices.GetRequiredService<SplytContext>();
        if (httpContext.Items.TryGetValue("FirebaseUserId", out var firebaseUserId))
        {
            if (firebaseUserId != null)
            {
                string firebaseUserIdString = firebaseUserId.ToString() ?? "";
                {
                    try
                    {
                        // Because of check, we know the below variable is not null
                        var user = await dbContext.User.FirstOrDefaultAsync(u => u.Id == firebaseUserIdString);
                        if (user == null)
                        {
                            user = new User
                            {
                                Id = firebaseUserIdString,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            dbContext.User.Add(user);
                            await dbContext.SaveChangesAsync();
                            logger.LogInformation("Find or create ran on: {userId}", user.Id);
                        }
                        httpContext.Items.Add("User", user);
                        await _next(httpContext);
                    }
                    catch (Exception e)
                    {
                        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await httpContext.Response.WriteAsync(e.Message);
                    }
                }
            }
        }
        else
        {
            logger.LogError("FirebaseUserId not found in httpContext.Items");
            await _next(httpContext);
        }
    }


}