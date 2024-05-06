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


    /*****************************************************************************************************************************
    // api/v1/groups [GET] - Get all groups DEVELOPMENT/TESTING ONLY
    ******************************************************************************************************************************/
    [HttpGet]
    public ActionResult<List<Group>> GetGroups()
    {

        _logger.LogInformation("GetGroups() called");

        try
        {
            List<Group> groups = _context.Group.ToList();
            return groups;
        }
        catch (Exception e)
        {
            _logger.LogError("GetGroups() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

    /*****************************************************************************************************************************
    // api/v1/groups/create [POST] - Creates a new group
    ******************************************************************************************************************************/
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
                CreatedById = firebaseUserId,
                Description = group.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };



            // Add the group to the database so the group member objects can reference the groupID
            _context.Group.Add(newGroup);
            _context.SaveChanges();

            int groupId = newGroup.Id;

            // create the group member object for the user who created the group
            GroupMember newGroupMember = new GroupMember
            {
                GroupId = groupId,
                MemberId = firebaseUserId,
                IsAdmin = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = GroupMemberStatus.Joined,
                InvitedById = firebaseUserId,
            };

            _context.GroupMember.Add(newGroupMember);
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
                CreatedById = newGroup.CreatedById,
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

    /*****************************************************************************************************************************
    // Get one group
    // api/v1/groups/{groupId} [GET]
    *****************************************************************************************************************************/
    [HttpGet("{groupId}", Name = "GetGroup")]
    public ActionResult<GroupWithoutUsersDTO> GetGroup(int groupId)
    {
        _logger.LogInformation("GetGroup() called");
        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;
        if (firebaseUserId == null)
        {
            return StatusCode(403, "Forbidden");
        }
        // Find the group
        Group? group = _context.Group.Find(groupId);
        if (group == null)
        {
            return NotFound("Group not found");
        }

        // Check if the user is a member of the group
        GroupMember? groupMember = _context.GroupMember.Find(groupId, firebaseUserId);
        if (groupMember == null)
        {
            return StatusCode(403, "Forbidden: You must be a member of the group to view it");
        }

        // Get the group members
        List<GroupMembersWithoutUser> groupMembersWithoutUser = new List<GroupMembersWithoutUser>();
        List<GroupMember> groupMembers = _context.GroupMember.Where(gm => gm.GroupId == groupId && gm.Status == GroupMemberStatus.Joined).ToList();
        foreach (var member in groupMembers)
        {
            GroupMembersWithoutUser groupMemberWithoutUser = new GroupMembersWithoutUser
            {
                GroupId = member.GroupId,
                MemberId = member.MemberId,
                IsAdmin = member.IsAdmin,
                Status = member.Status,
                InvitedById = member.InvitedById,
                CreatedAt = member.CreatedAt,
                UpdatedAt = member.UpdatedAt
            };
            groupMembersWithoutUser.Add(groupMemberWithoutUser);
        }

        // Shape the response object
        GroupWithoutUsersDTO response = new GroupWithoutUsersDTO
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            CreatedById = group.CreatedById,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt,
            GroupMembers = groupMembersWithoutUser
        };

        return response;
    }


    /*****************************************************************************************************************************
    // Add members to a group
    // api/v1/groups/{groupId}/addmembers [POST]
    *****************************************************************************************************************************/
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
            GroupMember? inviter = _context.GroupMember.Find(groupId, firebaseUserId);
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
                GroupMember? member = _context.GroupMember.Find(groupId, memberId);
                if (member != null && member.Status == GroupMemberStatus.Joined)
                {
                    return BadRequest($"A user you tried to add is already in the group");
                }
            }
            // Check to see if the invited user is banned
            foreach (var memberId in members.MemberIds)
            {
                GroupMember? member = _context.GroupMember.Find(groupId, memberId);
                if (member != null && member.Status == GroupMemberStatus.Banned)
                {
                    return BadRequest($"A user you tried to add is banned from the group");
                }
            }

            // Add the members to the group
            foreach (var memberId in members.MemberIds)
            {
                GroupMember newGroupMember = new GroupMember
                {
                    GroupId = groupId,
                    MemberId = memberId,
                    IsAdmin = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Status = GroupMemberStatus.Invited,
                    InvitedById = firebaseUserId,
                };

                _context.GroupMember.Add(newGroupMember);
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

    /*****************************************************************************************************************************
    Delete a group
    api/v1/groups/{groupId}/delete [DELETE]
    *****************************************************************************************************************************/
    [HttpDelete("{groupId}/delete", Name = "DeleteGroup")]
    public ActionResult DeleteGroup(int groupId)
    {
        _logger.LogInformation("DeleteGroup() called");
        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;
        if (firebaseUserId == null)
        {
            return StatusCode(403, "Forbidden");
        }
        try
        {
            using (var batchTransaction = _context.Database.BeginTransaction())
            {
                // Find the group
                Group? group = _context.Group.Find(groupId);
                if (group == null)
                {
                    return NotFound("Group not found");
                }

                // Check if the user is the creator of the group
                if (group.CreatedById != firebaseUserId)
                {
                    return StatusCode(403, "Forbidden: You must be the creator of the group to delete it");
                }

                // Delete all group members
                List<GroupMember> groupMembers = _context.GroupMember.Where(gm => gm.GroupId == groupId).ToList();
                foreach (var member in groupMembers)
                {
                    _context.GroupMember.Remove(member);
                }

                // Delete the group
                _context.Group.Remove(group);


                _context.SaveChanges();
                batchTransaction.Commit();

                return NoContent();
            }

        }
        catch (Exception e)
        {
            _logger.LogError("DeleteGroup() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

    /*****************************************************************************************************************************
    update a group
    api/v1/groups/{groupId}/update [PUT]
    *****************************************************************************************************************************/

    [HttpPut("{groupId}/update", Name = "UpdateGroup")]
    public ActionResult UpdateGroup(int groupId, [FromBody] UpdateGroupDTO group)
    {
        _logger.LogInformation("UpdateGroup() called");
        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;
        if (firebaseUserId == null)
        {
            return StatusCode(403, "Forbidden");
        }
        try
        {
            // Find the group
            Group? groupToUpdate = _context.Group.Find(groupId);
            if (groupToUpdate == null)
            {
                return NotFound("Group not found");
            }

            bool createdByUser = groupToUpdate.CreatedById == firebaseUserId;
            bool isAdmin = _context.GroupMember.Find(groupId, firebaseUserId)?.IsAdmin ?? false;

            if (!createdByUser && !isAdmin)
            {
                return StatusCode(403, "Forbidden: You must be the creator of the group or an admin to update it");
            }


            // Update the group
            groupToUpdate.Name = group.Name ?? groupToUpdate.Name;
            groupToUpdate.Description = group.Description ?? groupToUpdate.Description;
            groupToUpdate.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            return NoContent();
        }
        catch (Exception e)
        {
            _logger.LogError("UpdateGroup() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

    /*****************************************************************************************************************************
    Leave a group
    api/v1/groups/{groupId}/leave [DELETE]
    *****************************************************************************************************************************/
    [HttpDelete("{groupId}/leave", Name = "LeaveGroup")]
    public ActionResult LeaveGroup(int groupId)
    {
        _logger.LogInformation("LeaveGroup() called");
        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;
        if (firebaseUserId == null)
        {
            return StatusCode(403, "Forbidden");
        }
        try
        {
            // Find group
            Group? group = _context.Group.Find(groupId);
            if (group == null)
            {
                return NotFound("Group not found");
            }

            // Find group member
            GroupMember? groupMember = _context.GroupMember.Find(groupId, firebaseUserId);
            if (groupMember == null)
            {
                return StatusCode(403, "Forbidden: You must be a member of the group to leave it");
            }

            // Check if the user is the creator of the group
            if (group.CreatedById == firebaseUserId)
            {
                return StatusCode(403, "You cannot leave a group you created. Delete the group instead.");
            }

            // Check if the user is the last admin
            if (groupMember.IsAdmin)
            {
                List<GroupMember> groupMembers = _context.GroupMember.Where(gm => gm.GroupId == groupId && gm.Status == GroupMemberStatus.Joined && gm.IsAdmin == true).ToList();
                if (groupMembers.Count == 1)
                {
                    return StatusCode(403, "You are the last admin of the group. Promote another member to admin before leaving.");
                }
            }

            // If a user is banned, they cannot "leave" the group (To ensure they can not rejoin)
            if (groupMember.Status == GroupMemberStatus.Banned)
            {
                return StatusCode(403, "You are banned from the group.");
            }

            // Delete the group member
            _context.GroupMember.Remove(groupMember);

            _context.SaveChanges();

            return NoContent();
        }
        catch (Exception e)
        {
            _logger.LogError("LeaveGroup() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

    /*****************************************************************************************************************************
    Get a groups transactions
    api/v1/groups/{groupId}/transactions [GET]
    *****************************************************************************************************************************/
    [HttpGet("{groupId}/transactions", Name = "GetGroupTransactions")]
    public ActionResult<List<Transaction>> GetGroupTransaction(int groupId)
    {
        _logger.LogInformation("GetGroupTransaction() called");
        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;
        if (firebaseUserId == null)
        {
            return StatusCode(403, "Forbidden");
        }
        try
        {
            // Find group
            Group? group = _context.Group.Find(groupId);
            if (group == null)
            {
                return NotFound("Group not found");
            }

            // Check if the user is a member of the group and has status of "Joined"
            GroupMember? groupMember = _context.GroupMember.Find(groupId, firebaseUserId);
            if (groupMember == null || groupMember.Status != GroupMemberStatus.Joined)
            {
                return StatusCode(403, "Forbidden: You must be a member of the group to view its transactions");
            }

            // Get the transactions
            List<Transaction> transactions = _context.Transaction.Where(t => t.GroupId == groupId).ToList();

            return transactions;
        }
        catch (Exception e)
        {
            _logger.LogError("GetGroupTransaction() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

    /*****************************************************************************************************************************
    Remove a member from a group
    api/v1/groups/{groupId}/removemember [DELETE]
    *****************************************************************************************************************************/
    [HttpDelete("{groupId}/removemember/{userId}", Name = "RemoveMemberFromGroup")]
    public ActionResult RemoveMember(int groupId, string userId)
    {
        _logger.LogInformation("RemoveMember() called");
        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;
        if (firebaseUserId == null)
        {
            return StatusCode(403, "Forbidden");
        }
        try
        {
            // Find the group
            Group? group = _context.Group.Find(groupId);
            if (group == null)
            {
                return NotFound("Group not found");
            }

            bool isCreator = group.CreatedById == firebaseUserId;
            bool isAdminAndMember = _context.GroupMember.Find(groupId, firebaseUserId)?.IsAdmin == true &&
                                    _context.GroupMember.Find(groupId, firebaseUserId)?.Status == GroupMemberStatus.Joined;
            bool removingSelf = firebaseUserId == userId;
            if (!isCreator && !isAdminAndMember && !removingSelf)
            {
                return StatusCode(403, "You do not have permission to remove this member from the group");
            }

            // Find the member to remove
            GroupMember? memberToRemove = _context.GroupMember.Find(groupId, userId);
            if (memberToRemove == null)
            {
                return NotFound("User not found in group");
            }
            // Check if user is in the group
            if (memberToRemove.Status != GroupMemberStatus.Joined)
            {
                return BadRequest("User is not in the group");
            }

            // Check if the user is the creator of the group
            if (group.CreatedById == userId)
            {
                return StatusCode(403, "You cannot remove the creator of the group");
            }

            // Check if the user is the last admin
            if (memberToRemove.IsAdmin)
            {
                List<GroupMember> groupMembers = _context.GroupMember.Where(gm => gm.GroupId == groupId && gm.Status == GroupMemberStatus.Joined && gm.IsAdmin == true).ToList();
                if (groupMembers.Count == 1)
                {
                    return StatusCode(403, "You are trying to remove the last admin of the group. Promote another member to admin before removing.");
                }
            }

            // Remove the member
            _context.GroupMember.Remove(memberToRemove);
            _context.SaveChanges();

            return NoContent();
        }
        catch (Exception e)
        {
            _logger.LogError("RemoveMember() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

    /*****************************************************************************************************************************
    get members of a group
    api/v1/groups/{groupId}/members [GET]
    *****************************************************************************************************************************/
    [HttpGet("{groupId}/members", Name = "GetGroupMembers")]
    public ActionResult<List<GroupMember>> GetGroupMembers(int groupId)
    {
        _logger.LogInformation("GetGroupMembers() called");
        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;
        if (firebaseUserId == null)
        {
            return StatusCode(403, "Forbidden");
        }
        try
        {
            // Find the group
            Group? group = _context.Group.Find(groupId);
            if (group == null)
            {
                return NotFound("Group not found");
            }

            // Check if the user is a member of the group
            GroupMember? groupMember = _context.GroupMember.Find(groupId, firebaseUserId);
            if (groupMember == null || groupMember.Status != GroupMemberStatus.Joined)
            {
                return StatusCode(403, "Forbidden: You must be a member of the group to view its members");
            }

            // Get the group members
            List<GroupMember> groupMembers = _context.GroupMember.Where(gm => gm.GroupId == groupId).Include(gm => gm.Member).ToList();

            return groupMembers;
        }
        catch (Exception e)
        {
            _logger.LogError("GetGroupMembers() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

    /*****************************************************************************************************************************
    change a members status in a group
    api/v1/groups/{groupId}/changestatus/{userId} [PUT]
    *****************************************************************************************************************************/
    [HttpPut("{groupId}/changestatus/{userId}", Name = "ChangeMemberStatus")]
    public ActionResult ChangeMemberStatus(int groupId, string userId, [FromBody] GroupMemberStatus newStatus)
    {
        _logger.LogInformation("ChangeMemberStatus() called");
        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;
        if (firebaseUserId == null)
        {
            return StatusCode(403, "Forbidden");
        }


        try
        {
            Group? group = _context.Group.Find(groupId);
            if (group == null)
            {
                return NotFound("Group not found");
            }

            // Check role of user
            GroupMember? groupMember = _context.GroupMember.Find(groupId, firebaseUserId);
            if (groupMember == null)
            {
                return StatusCode(403, "Forbidden: You must be a member of the group to change a member's status");
            }

            if (groupMember.Status == GroupMemberStatus.Banned)
            {
                return StatusCode(403, "Forbidden: you are banned from the group");
            }
            else if (groupMember.Status == GroupMemberStatus.Invited)
            {
                // an invited user can only accept or decline their own invite
                if (firebaseUserId != userId)
                {
                    return StatusCode(403, "Forbidden: You must accept or decline your own invite");
                }
                if (newStatus != GroupMemberStatus.Joined && newStatus != GroupMemberStatus.Declined)
                {
                    return BadRequest("Forbidden: You can only accept or decline an invite");
                }
                groupMember.Status = newStatus;
                groupMember.UpdatedAt = DateTime.UtcNow;
            }
            else if (groupMember.Status == GroupMemberStatus.Joined)
            {
                if (!groupMember.IsAdmin && group.CreatedById != firebaseUserId)
                {
                    return StatusCode(403, "Forbidden: You must be an admin or the creator of the group to change a member's status");
                }
                if (group.CreatedById == userId)
                {
                    return StatusCode(403, "Forbidden: You cannot change the status of the creator of the group");
                }
                if (groupMember.MemberId == userId)
                {
                    return StatusCode(403, "Forbidden: You cannot change your own status");
                }
                GroupMember? memberToChange = _context.GroupMember.Find(groupId, userId);
                if (memberToChange == null)
                {
                    return NotFound("User not found in group");
                }
                if (memberToChange.Status == GroupMemberStatus.Banned || memberToChange.Status == GroupMemberStatus.Invited || memberToChange.Status == GroupMemberStatus.Left || memberToChange.Status == GroupMemberStatus.Declined || memberToChange.Status == GroupMemberStatus.Kicked)
                {
                    return StatusCode(403, "Forbidden: You cannot change the status of a banned, invited, left, declined, or removed user");
                }

                // Only allow users to kick or ban users
                if (newStatus != GroupMemberStatus.Kicked && newStatus != GroupMemberStatus.Banned)
                {
                    return BadRequest("Forbidden: You can only kick or ban a user");
                }
                memberToChange.Status = newStatus;
                memberToChange.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                return StatusCode(403, "Forbidden: You must be a member of the group to change a member's status");
            }



            _context.SaveChanges();

            return NoContent();
        }
        catch (Exception e)
        {
            _logger.LogError("ChangeMemberStatus() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

}