using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Splyt.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]

public class GroupsController : ControllerBase
{
    private readonly ILogger<GroupsController> _logger;
    private readonly SplytContext _context;

    public GroupsController(SplytContext context, ILogger<GroupsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /*
    
    *****************
    MOVE THIS TO THE USERS CONTROLLER UNDER USERS/USERID/GROUPS
    *****************
    */
    // api/v1/groups [GET] - Returns a list of groups
    [HttpGet(Name = "GetUserGroups")]
    public ActionResult<List<GroupWithoutUsersDTO>> GetUserGroups()
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
            var groupsDto = new List<GroupWithoutUsersDTO>();
            foreach (var group in groups)
            {
                GroupWithoutUsersDTO groupWithoutUsers = new GroupWithoutUsersDTO()
                {
                    Id = group.Id,
                    Name = group.Name,
                    Description = group.Description,
                    CreatedBy = group.CreatedBy,
                    CreatedAt = group.CreatedAt,
                    UpdatedAt = group.UpdatedAt,
                    GroupMembers = group.GroupMembers
                        .Select(gm => new GroupMembersWithoutUser()
                        {
                            GroupId = gm.GroupId,
                            MemberId = gm.MemberId,
                            IsAdmin = gm.IsAdmin,
                            Status = gm.Status,
                            InvitedById = gm.InvitedById,
                            CreatedAt = gm.CreatedAt,
                            UpdatedAt = gm.UpdatedAt
                        })
                        .ToList()
                };

                // Add the DTO to the list
                groupsDto.Add(groupWithoutUsers);
            }

            return groupsDto;
        }

        catch (Exception e)
        {
            _logger.LogError("GetGroups() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

    // api/v1/groups/create [POST] - Creates a new group
    [HttpPost("create", Name = "CreateGroup")]
    public ActionResult<GroupWithoutUsersDTO> CreateGroup(CreateGroupDTO group)
    {
        _logger.LogInformation("CreateGroup() called");

        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;
        if (firebaseUserId == null)
        {
            return StatusCode(403, "Forbidden");
        }
        try
        {
            // begin constructing the group object
            Group newGroup = new Group
            {
                Name = group.Name,
                CreatedBy = firebaseUserId,
                Description = group.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };



            // Add the group to the database so the group member objects can reference the groupID
            _context.Group.Add(newGroup);
            _context.SaveChanges();

            int groupId = newGroup.Id;

            // create the group member object for the user who created the group
            GroupMembers newGroupMember = new GroupMembers
            {
                GroupId = groupId,
                MemberId = firebaseUserId,
                IsAdmin = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = GroupMemberStatus.Joined,
                InvitedById = firebaseUserId,
            };

            _context.GroupMembers.Add(newGroupMember);
            _context.SaveChanges();

            // Shape the response object
            var groupMembersWithoutUser = new GroupMembersWithoutUser
            {
                GroupId = newGroupMember.GroupId,
                MemberId = newGroupMember.MemberId,
                IsAdmin = newGroupMember.IsAdmin,
                Status = newGroupMember.Status,
                InvitedById = newGroupMember.InvitedById,
                CreatedAt = newGroupMember.CreatedAt,
                UpdatedAt = newGroupMember.UpdatedAt
            };

            GroupWithoutUsersDTO response = new GroupWithoutUsersDTO
            {
                Id = newGroup.Id,
                Name = newGroup.Name,
                Description = newGroup.Description,
                CreatedBy = newGroup.CreatedBy,
                CreatedAt = newGroup.CreatedAt,
                UpdatedAt = newGroup.UpdatedAt,
                GroupMembers = new List<GroupMembersWithoutUser> { groupMembersWithoutUser }
            };

            return CreatedAtRoute("CreateGroup", new { id = newGroup.Id }, response);


        }
        catch (Exception e)
        {
            _logger.LogError("CreateGroup() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

    // Add members to a group
    // api/v1/groups/{groupId}/addmembers [POST]
    [HttpPost("{groupId}/addmembers", Name = "AddMembersToGroup")]
    public ActionResult AddMembersToGroup(int groupId, AddMembersDTO members)
    {
        _logger.LogInformation("AddMembersToGroup() called");
        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;
        if (firebaseUserId == null)
        {
            return StatusCode(403, "Must be logged in to add members to a group");
        }
        try
        {
            // Check if group exists
            Group? group = _context.Group.Find(groupId);
            if (group == null)
            {
                return NotFound("Group not found");
            }
            // Check if member who is inviting others is part of the group
            GroupMembers? inviter = _context.GroupMembers.Find(groupId, firebaseUserId);
            if (inviter == null || inviter.Status != GroupMemberStatus.Joined)
            {
                return StatusCode(403, "Forbidden: You must be a member of the group to add others to it");
            }

            // Check if the members to be added are on the platform
            foreach (var memberId in members.MemberIds)
            {
                User? user = _context.User.Find(memberId);
                if (user == null)
                {
                    return NotFound($"One of the users you tried to add to the group does not exist.");
                }
            }
            // Check if members are already in the group (must have accepted invite to be in the group)
            foreach (var memberId in members.MemberIds)
            {
                GroupMembers? member = _context.GroupMembers.Find(groupId, memberId);
                if (member != null && member.Status == GroupMemberStatus.Joined)
                {
                    return BadRequest($"A user you tried to add is already in the group");
                }
            }
            // Check to see if the invited user is banned
            foreach (var memberId in members.MemberIds)
            {
                GroupMembers? member = _context.GroupMembers.Find(groupId, memberId);
                if (member != null && member.Status == GroupMemberStatus.Banned)
                {
                    return BadRequest($"A user you tried to add is banned from the group");
                }
            }

            // Add the members to the group
            foreach (var memberId in members.MemberIds)
            {
                GroupMembers newGroupMember = new GroupMembers
                {
                    GroupId = groupId,
                    MemberId = memberId,
                    IsAdmin = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Status = GroupMemberStatus.Invited,
                    InvitedById = firebaseUserId,
                };

                _context.GroupMembers.Add(newGroupMember);
                _context.SaveChanges();
            }

            // Return 204 No Content, indicating success but no response
            return NoContent();

        }
        catch (Exception e)
        {
            _logger.LogError("AddMembersToGroup() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }


}