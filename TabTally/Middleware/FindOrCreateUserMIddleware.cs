using Microsoft.EntityFrameworkCore;

public class FindOrCreateUserMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SplytContext _context;

    public FindOrCreateUserMiddleware(RequestDelegate next, SplytContext context)
    {
        _next = next;
        _context = context;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Items.TryGetValue("FirebaseUserId", out var firebaseUserId))
            if (firebaseUserId != null)
            {
                {
                    // Because of check, we know the below variable is not null
                    string firebaseUserIdString = firebaseUserId.ToString() ?? "";
                    var user = await _context.User.FirstOrDefaultAsync(u => u.Id == firebaseUserIdString);
                    if (user == null)
                    {
                        user = new User
                        {
                            Id = firebaseUserIdString,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.User.Add(user);
                        await _context.SaveChangesAsync();
                    }
                    context.Items.Add("User", user);
                    await _next(context);
                }
            }
    }


}