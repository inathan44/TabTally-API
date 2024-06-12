using System.Text.RegularExpressions;
using FirebaseAdmin.Auth;
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

    /************************************************************************************************************
    // api/v1/Users [GET] - Returns a list of all users
    NOTE THIS ROUTE IS STRICTLY FOR TESTING PURPOSES AND SHOULD BE REMOVED IN PRODUCTION
    ************************************************************************************************************/
    [HttpGet]
    public ActionResult<List<User>> GetUsers()
    {
        if (Environment.GetEnvironmentVariable("ENVIRONMENT") != "development")
        {
            return StatusCode(403, "Forbidden");
        }
        _logger.LogInformation("GetUsers() called");

        try
        {
            return _context.User.ToList();
        }
        catch (Exception e)
        {
            _logger.LogError("GetUsers() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
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




    /************************************************************************************************************
    NEEDED: ADD FIREBASE INTO THIS ROUTE AS EMAIL CHANGES NEEDS TO ALSO BE CHANGED IN FIREBASE

    /************************************************************************************************************
    // api/v1/Users/id/update [Put] - update the user object
    ************************************************************************************************************/
    [HttpPut("{id}/update")]
    public IActionResult UpdateUser(string id, UpdateUserRequestDTO user)
    {
        _logger.LogInformation("UpdateUser() called");

        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;

        // Users can only update their own user record
        if (firebaseUserId != id)
        {
            return StatusCode(403, "can only update your own user");
        }

        using (var batchTransaction = _context.Database.BeginTransaction())
        {
            try
            {

                var existingUser = _context.User.FirstOrDefault(u => u.Id == firebaseUserId);
                if (existingUser == null)
                {
                    return NotFound("User not found when updating: " + firebaseUserId);
                }

                // Add regex to ensure alphanumeric characters only, no profanity, etc.
                if (user.Username != null)
                {

                    var existingUserWithSameUsername = _context.User.FirstOrDefault(u => u.Username.ToLower() == user.Username.ToLower());
                    if (existingUserWithSameUsername != null)
                    {
                        return BadRequest("Username already in use");
                    }


                    Regex usernameRegex = new Regex(@"^[a-zA-Z0-9\.\-_]+$");
                    if (!usernameRegex.IsMatch(user.Username))
                    {
                        return BadRequest("Invalid username");
                    }
                    existingUser.Username = user.Username;
                    _context.SaveChanges();
                }
                // Add regex to ensure valid email format
                if (user.Email != null)
                {
                    // Use regex to validate email format
                    string emailPattern = @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$";
                    if (!Regex.IsMatch(user.Email, emailPattern))
                    {
                        return BadRequest("Invalid email");
                    }

                    if (_context.User.Any(u => u.Email == user.Email))
                    {
                        return BadRequest("Email already in use");
                    }

                    existingUser.Email = user.Email;
                    _context.SaveChanges();
                }
                // Can not be empty string
                if (user.FirstName != null)
                {
                    existingUser.FirstName = user.FirstName;
                    _context.SaveChanges();
                }
                // Can not be empty string
                if (user.LastName != null)
                {
                    existingUser.LastName = user.LastName;
                    _context.SaveChanges();
                }
                existingUser.UpdatedAt = DateTime.UtcNow;
                _context.SaveChanges();

                batchTransaction.Commit();

                return Ok("User updated");
            }
            catch (Exception e)
            {
                _logger.LogError("UpdateUser() failed with exception: {0}", e);
                return StatusCode(500, $"Internal server error: {e.Message}");
            }
        }
    }
    /************************************************************************************************************
    // api/v1/Users/groups [GET] - Returns a list of groups
    ************************************************************************************************************/
    [HttpGet("groups", Name = "GetUserGroups")]
    public ActionResult<List<GetUserGroupsResponse>> GetUserGroups()
    {
        _logger.LogInformation("GetGroups() called");
        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;
        if (firebaseUserId == null)
        {
            return StatusCode(403, "Forbidden");
        }

        try
        {
            // Get all groups this user is a member of
            Group[] groups = _context.Group.Include(g => g.GroupMembers).Where(g => g.GroupMembers.Any(gm => gm.MemberId == firebaseUserId)).ToArray();


            // Shape the response object
            var userGroupsResponse = new List<GetUserGroupsResponse>();
            foreach (var group in groups)
            {
                // Find creator of the group
                var creator = _context.User.FirstOrDefault(u => u.Id == group.CreatedById);

                GetUserGroupsResponse userGroup = new GetUserGroupsResponse()
                {
                    Id = group.Id,
                    Name = group.Name,
                    Description = group.Description,
                    CreatedById = group.CreatedById,
                    CreatedBy = new UserSummaryDTO()
                    {
                        Id = creator.Id,
                        Username = creator.Username,
                        FirstName = creator.FirstName,
                        LastName = creator.LastName,
                        CreatedAt = creator.CreatedAt,
                        UpdatedAt = creator.UpdatedAt
                    },
                    CreatedAt = group.CreatedAt,
                    UpdatedAt = group.UpdatedAt,
                    GroupMembers = group.GroupMembers
                        .Select(gm => new GetUserGroupsGroupMemberDTO()
                        {
                            Id = gm.Id,
                            GroupId = gm.GroupId,
                            MemberId = gm.MemberId,
                            InvitedById = gm.InvitedById,
                            IsAdmin = gm.IsAdmin,
                            Status = gm.Status,
                            CreatedAt = gm.CreatedAt,
                            UpdatedAt = gm.UpdatedAt,
                        })
                        .ToList()
                };

                // Add the DTO to the list
                userGroupsResponse.Add(userGroup);
            }

            return userGroupsResponse;
        }

        catch (Exception e)
        {
            _logger.LogError("GetGroups() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

    /************************************************************************************************************
    // api/v1/Users/{id}/delete [DELETE] - Deletes a user
    NOTE: THIS ROUTE IS USED MAINLY FOR UNIT TESTING PURPOSES OR FOR WHEN A USER WANTS TO DELETE THEIR OWN ACCOUNT
    MEANING THIS SHOULD BE VERY PROTECTED AND NOT EASILY ACCESSIBLE, DELETING A USER SHOULD CASCADE DELETE ALL
    ************************************************************************************************************/
    [HttpDelete("{id}/delete", Name = "DeleteUser")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;

        // Users can only delete their own user record
        if (firebaseUserId != id)
        {
            return StatusCode(403, "you can only delete your own user record");
        }
        using (var batchTransaction = _context.Database.BeginTransaction())
        {
            try
            {
                _logger.LogInformation("DeleteUser() called");

                var existingUser = _context.User.FirstOrDefault(u => u.Id == firebaseUserId);
                if (existingUser == null)
                {
                    return NotFound("User not found when deleting: " + firebaseUserId);
                }

                // Delete the user from Firebase
                var auth = FirebaseAuth.DefaultInstance;
                await auth.DeleteUserAsync(firebaseUserId);

                // If the Firebase operation succeeded, commit the transaction
                _context.User.Remove(existingUser);
                _context.SaveChanges();
                batchTransaction.Commit();

                _logger.LogInformation("User deleted: {0}", firebaseUserId);
                return Ok("User deleted");
            }
            catch (FirebaseAuthException e)
            {
                _logger.LogError("DeleteUser() failed to delete user from firebase with exception: {0}", e);
                return StatusCode(500, $"Internal server error: {e.Message}");
            }
            catch (Exception e)
            {
                _logger.LogError("DeleteUser() failed with exception: {0}", e);
                return StatusCode(500, $"Internal server error: {e.Message}");
            }
        }
    }
    /************************************************************************************************************
    // api/v1/Users/{id} [Get] - Retrieves the signed in user's information
    ************************************************************************************************************/
    [HttpGet("{id}", Name = "GetUser")]
    public ActionResult<GetUserResponseDTO> GetUser(string id)
    {
        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;

        // Users can only view their own user record
        if (firebaseUserId != id)
        {
            return StatusCode(403, "Forbidden");
        }

        try
        {
            _logger.LogInformation("GetUser() called");

            // get group members for the user
            GetUserGroupMemberDTO[] groupMembers = _context.GroupMember
                .Where(gm => gm.MemberId == firebaseUserId)
                .Select(gm => new GetUserGroupMemberDTO()
                {
                    Id = gm.Id,
                    GroupId = gm.GroupId,
                    MemberId = gm.MemberId,
                    InvitedById = gm.InvitedById,
                    IsAdmin = gm.IsAdmin,
                    Status = gm.Status,
                    CreatedAt = gm.CreatedAt,
                    UpdatedAt = gm.UpdatedAt,
                    Group = new GetUserGroupDTO()
                    {
                        Id = gm.Group.Id,
                        Name = gm.Group.Name,
                        Description = gm.Group.Description,
                        CreatedAt = gm.Group.CreatedAt,
                        UpdatedAt = gm.Group.UpdatedAt,
                        GroupMembers = gm.Group.GroupMembers
                            .Select(gm => new GetUserGroupMemberDTO()
                            {
                                Id = gm.Id,
                                GroupId = gm.GroupId,
                                MemberId = gm.MemberId,
                                InvitedById = gm.InvitedById,
                                IsAdmin = gm.IsAdmin,
                                Status = gm.Status,
                                CreatedAt = gm.CreatedAt,
                                UpdatedAt = gm.UpdatedAt
                            })
                            .ToList()
                    }

                })
                .ToArray();



            var user = _context.User.FirstOrDefault(u => u.Id == firebaseUserId);
            if (user == null)
            {
                return NotFound("User not found: " + firebaseUserId);
            }

            return new GetUserResponseDTO()
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                GroupMembers = groupMembers
            };
        }
        catch (Exception e)
        {
            _logger.LogError("GetUser() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

    /************************************************************************************************************
    // api/v1/Users [Get] - Retrieves a user's transactions
    ************************************************************************************************************/
    // [HttpGet("transactions", Name = "GetUserTransactions")]
    // public ActionResult<List<Transaction>> GetUserTransactions()
    // {
    //     _logger.LogInformation("GetUserTransactions() called");
    //     var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;
    //     if (firebaseUserId == null)
    //     {
    //         return StatusCode(403, "Forbidden");
    //     }

    //     try
    //     {
    //         // Find all transaction details that include the user as recipient or payer
    //         var transactions = _context.Transaction
    //             .Include(t => t.TransactionDetails)
    //             .Where(t => t.TransactionDetails.Any(td => td.PayerId == firebaseUserId || td.RecipientId == firebaseUserId))
    //             .ToList();



    //         return transactions;
    //     }
    //     catch (Exception e)
    //     {
    //         _logger.LogError("GetUserTransactions() failed with exception: {0}", e);
    //         return StatusCode(500, $"Internal server error: {e.Message}");
    //     }
    // }
}
