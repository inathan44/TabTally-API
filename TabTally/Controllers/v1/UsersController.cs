using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Splyt.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]

public class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly SplytContext _context;

    public UsersController(SplytContext context, ILogger<UsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // api/v1/users/create [POST] - Creates a new user
    [HttpPost("create", Name = "CreateUser")]
    public ActionResult<User> CreateUser(User user)
    {
        try
        {
            _logger.LogInformation("CreateUser() called");

            _context.User.Add(user);
            _context.SaveChanges();

            return CreatedAtRoute("GetUser", new { id = user.Id }, user);
        }
        catch (Exception e)
        {
            _logger.LogError("CreateUser() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

    [HttpPut("{id}/update")]
    public IActionResult UpdateUser(string id, UpdateUserRequestDTO user)
    {
        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;

        // Users can only update their own user record
        if (firebaseUserId != id)
        {
            return StatusCode(403, "Forbidden");
        }

        try
        {
            _logger.LogInformation("UpdateUser() called");

            var existingUser = _context.User.FirstOrDefault(u => u.Id == firebaseUserId);
            if (existingUser == null)
            {
                return NotFound("User not found: " + firebaseUserId);
            }

            // Add regex to ensure alphanumeric characters only, no profanity, etc.
            if (user.Username != null)
            {
                existingUser.Username = user.Username;
            }
            // Add regex to ensure valid email format
            if (user.Email != null)
            {
                existingUser.Email = user.Email;
            }
            // Can not be empty string
            if (user.FirstName != null)
            {
                existingUser.FirstName = user.FirstName;
            }
            // Can not be empty string
            if (user.LastName != null)
            {
                existingUser.LastName = user.LastName;
            }
            existingUser.UpdatedAt = DateTime.UtcNow;

            _context.User.Update(existingUser);
            _context.SaveChanges();

            return NoContent();
        }
        catch (Exception e)
        {
            _logger.LogError("UpdateUser() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }
}