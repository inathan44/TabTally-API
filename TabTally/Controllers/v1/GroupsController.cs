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
            List<Group> groups = _context.Group.Include(g => g.CreatedBy).ToList();
            return groups;
        }
        catch (Exception e)
        {
            _logger.LogError("GetGroups() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

    /*****************************************************************************************************************************
    // api/v1/Groups/members [GET] - Get all group members DEVELOPMENT/TESTING ONLY
    ******************************************************************************************************************************/
    [HttpGet("members")]
    public ActionResult<List<GroupMember>> GetGroupMembers()
    {
        _logger.LogInformation("GetGroupMembers() called");

        try
        {
            List<GroupMember> groupMembers = _context.GroupMember.ToList();
            return groupMembers;
        }
        catch (Exception e)
        {
            _logger.LogError("GetGroupMembers() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

    /*****************************************************************************************************************************
    // api/v1/groups/create [POST] - Creates a new group
    ******************************************************************************************************************************/
    [HttpPost("create", Name = "CreateGroup")]
    public ActionResult<GetGroupResponseDTO> CreateGroup(CreateGroupDTO group)
    {
        _logger.LogInformation("CreateGroup() called");

        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;
        if (firebaseUserId == null)
        {
            return StatusCode(403, "Forbidden");
        }
        using (var batchTransaction = _context.Database.BeginTransaction())
        {
            try
            {
                if (group.Name == null || group.Name.Length < 1 || group.Name.Length > 50)
                {
                    return BadRequest("Group name must be between 1 and 50 characters");
                }
                if (group.Description != null && group.Description.Length > 255)
                {
                    return BadRequest("Group description must be less than 255 characters");
                }
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

                batchTransaction.Commit();

                // Shape the response object
                var groupMembersWithoutUser = new GetGroupGroupMemberDTO
                {
                    Id = newGroupMember.Id,
                    GroupId = newGroupMember.GroupId,
                    MemberId = newGroupMember.MemberId,
                    Member = new UserSummaryDTO
                    {
                        Id = newGroupMember.Member.Id,
                        FirstName = newGroupMember.Member.FirstName,
                        LastName = newGroupMember.Member.LastName,
                        Username = newGroupMember.Member.Username,
                        CreatedAt = newGroupMember.Member.CreatedAt,
                        UpdatedAt = newGroupMember.Member.UpdatedAt
                    },
                    InvitedById = newGroupMember.InvitedById,
                    IsAdmin = newGroupMember.IsAdmin,
                    Status = newGroupMember.Status,
                    CreatedAt = newGroupMember.CreatedAt,
                    UpdatedAt = newGroupMember.UpdatedAt
                };

                GetGroupResponseDTO response = new GetGroupResponseDTO
                {
                    Id = newGroup.Id,
                    Name = newGroup.Name,
                    Description = newGroup.Description,
                    CreatedById = newGroup.CreatedById,
                    CreatedBy = new UserSummaryDTO
                    {
                        Id = newGroup.CreatedBy.Id,
                        FirstName = newGroup.CreatedBy.FirstName,
                        LastName = newGroup.CreatedBy.LastName,
                        Username = newGroup.CreatedBy.Username,
                        CreatedAt = newGroup.CreatedBy.CreatedAt,
                        UpdatedAt = newGroup.CreatedBy.UpdatedAt
                    },
                    CreatedAt = newGroup.CreatedAt,
                    UpdatedAt = newGroup.UpdatedAt,
                    GroupMembers = new List<GetGroupGroupMemberDTO> { groupMembersWithoutUser },
                    Transactions = new List<TransactionSummaryDTO>()
                };

                return CreatedAtRoute("GetGroup", new { groupId = newGroup.Id }, response);


            }
            catch (Exception e)
            {
                _logger.LogError("CreateGroup() failed with exception: {0}", e);
                return StatusCode(500, $"Internal server error: {e.Message}");
            }
        }
    }

    /*****************************************************************************************************************************
    // Get one group
    // api/v1/groups/{groupId} [GET]
    *****************************************************************************************************************************/
    [HttpGet("{groupId}", Name = "GetGroup")]
    public ActionResult<GetGroupResponseDTO> GetGroup(int groupId)
    {
        _logger.LogInformation("GetGroup() called");
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
            GroupMember? groupMember = _context.GroupMember
                .FirstOrDefault(gm => gm.GroupId == groupId && gm.MemberId == firebaseUserId);

            if (groupMember == null || groupMember.Status != GroupMemberStatus.Joined)
            {
                return StatusCode(403, "Forbidden: You must be a member of the group to view it");
            }

            _logger.LogInformation("Group found: {0}", group.Name);

            // Get the group members
            List<GetGroupGroupMemberDTO> groupMembersResponse = new List<GetGroupGroupMemberDTO>();
            List<GroupMember> groupMembers = _context.GroupMember
                .Include(gm => gm.Member)
                .Where(gm => gm.GroupId == groupId && gm.Status == GroupMemberStatus.Joined)
                .ToList();

            _logger.LogInformation("Group members found: {0}", groupMembers.Count);

            foreach (var member in groupMembers)
            {
                GetGroupGroupMemberDTO gm = new GetGroupGroupMemberDTO
                {
                    Id = member.Id,
                    GroupId = member.GroupId,
                    MemberId = member.MemberId,
                    Member = new UserSummaryDTO
                    {
                        Id = member.Member.Id,
                        FirstName = member.Member.FirstName,
                        LastName = member.Member.LastName,
                        Username = member.Member.Username,
                        CreatedAt = member.Member.CreatedAt,
                        UpdatedAt = member.Member.UpdatedAt
                    },
                    IsAdmin = member.IsAdmin,
                    Status = member.Status,
                    InvitedById = member.InvitedById,
                    CreatedAt = member.CreatedAt,
                    UpdatedAt = member.UpdatedAt
                };
                groupMembersResponse.Add(gm);
            }

            _logger.LogInformation("Group members response created");

            // Get the group transactions
            List<Transaction> transactions = _context.Transaction
                .Where(t => t.GroupId == groupId)
                .Include(t => t.TransactionDetails)
                .ToList();

            _logger.LogInformation("Transactions found: {0}", transactions.Count);

            List<TransactionSummaryDTO> transactionsResponse = new List<TransactionSummaryDTO>(
                transactions.Select(t => new TransactionSummaryDTO
                {
                    Id = t.Id,
                    CreatedById = t.CreatedById,
                    Amount = t.Amount,
                    CreatedBy = t.CreatedBy == null ? null : new UserSummaryDTO
                    {
                        Id = t.CreatedBy.Id,
                        FirstName = t.CreatedBy.FirstName,
                        LastName = t.CreatedBy.LastName,
                        Username = t.CreatedBy.Username,
                        CreatedAt = t.CreatedBy.CreatedAt,
                        UpdatedAt = t.CreatedBy.UpdatedAt
                    },
                    payerId = t.PayerId,
                    Payer = t.Payer == null ? null : new UserSummaryDTO
                    {
                        Id = t.Payer.Id,
                        FirstName = t.Payer.FirstName,
                        LastName = t.Payer.LastName,
                        Username = t.Payer.Username,
                        CreatedAt = t.Payer.CreatedAt,
                        UpdatedAt = t.Payer.UpdatedAt
                    },
                    Description = t.Description,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    GroupId = t.GroupId,
                    TransactionDetails = new List<TransactionDetailsSummaryDTO>(
                        t.TransactionDetails.Select(td => new TransactionDetailsSummaryDTO
                        {
                            Id = td.Id,
                            TransactionId = td.TransactionId,
                            PayerId = td.PayerId,
                            Payer = td.Payer == null ? null : new UserSummaryDTO
                            {
                                Id = td.Payer.Id,
                                FirstName = td.Payer.FirstName,
                                LastName = td.Payer.LastName,
                                Username = td.Payer.Username,
                                CreatedAt = td.Payer.CreatedAt,
                                UpdatedAt = td.Payer.UpdatedAt
                            },
                            RecipientId = td.RecipientId,
                            Recipient = td.Recipient == null ? null : new UserSummaryDTO
                            {
                                Id = td.Recipient.Id,
                                FirstName = td.Recipient.FirstName,
                                LastName = td.Recipient.LastName,
                                Username = td.Recipient.Username,
                                CreatedAt = td.Recipient.CreatedAt,
                                UpdatedAt = td.Recipient.UpdatedAt
                            },
                            Amount = td.Amount,
                        }

            ))
                }));

            // Shape the response object
            GetGroupResponseDTO response = new GetGroupResponseDTO
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                CreatedById = group.CreatedById,
                CreatedBy = new UserSummaryDTO
                {
                    Id = group.CreatedBy.Id,
                    FirstName = group.CreatedBy.FirstName,
                    LastName = group.CreatedBy.LastName,
                    Username = group.CreatedBy.Username,
                    CreatedAt = group.CreatedBy.CreatedAt,
                    UpdatedAt = group.CreatedBy.UpdatedAt
                },
                CreatedAt = group.CreatedAt,
                UpdatedAt = group.UpdatedAt,
                GroupMembers = groupMembersResponse,
                Transactions = transactionsResponse

            };

            _logger.LogInformation("Response object created");

            return response;
        }
        catch (Exception e)
        {
            _logger.LogError("GetGroup() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
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

        if (members.MemberIds == null || members.MemberIds.Count == 0)
        {
            return BadRequest("You must provide at least one member to add to the group");
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
            GroupMember? inviter = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == groupId && gm.MemberId == firebaseUserId);
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
                    return NotFound($"One of the users you tried to add to the group does not exist");
                }
            }

            foreach (var memberId in members.MemberIds)
            {
                GroupMember? groupMember = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == groupId && gm.MemberId == memberId);

                if (groupMember != null && groupMember.Status == GroupMemberStatus.Joined)
                {
                    return BadRequest($"A user you tried to add is already in the group");
                }
                if (groupMember != null && groupMember.Status == GroupMemberStatus.Invited)
                {
                    return BadRequest($"A user you tried to add has already been invited to the group");
                }
                if (groupMember != null && groupMember.Status == GroupMemberStatus.Banned)
                {
                    return BadRequest($"A user you tried to add is banned from the group");
                }

                GroupMember? existingGroupMember = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == groupId && gm.MemberId == memberId);

                if (existingGroupMember != null)
                {
                    // Update the existing GroupMember
                    existingGroupMember.IsAdmin = false;
                    existingGroupMember.UpdatedAt = DateTime.UtcNow;
                    existingGroupMember.Status = GroupMemberStatus.Invited;
                    existingGroupMember.InvitedById = firebaseUserId;
                }
                else
                {
                    // Create a new GroupMember
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
                }

            }



            _context.SaveChanges();

            int newMembersAdded = members.MemberIds.Count;
            return Ok($"{newMembersAdded} members added to the group");

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

                return Ok("Group deleted");
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

            if (group.Name == null && group.Description == null)
            {
                return BadRequest("You must provide a name or description to update the group");
            }

            if (group.Name != null && (group.Name.Length < 1 || group.Name.Length > 50))
            {
                return BadRequest("Group name must be between 1 and 50 characters");
            }

            if (group.Description != null && group.Description.Length > 255)
            {
                return BadRequest("Group description must be less than 255 characters");
            }

            GroupMember? groupMember = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == groupId && gm.MemberId == firebaseUserId);

            if (groupMember == null || groupMember.Status != GroupMemberStatus.Joined)
            {
                return StatusCode(403, "Forbidden: You must be a member of the group to update it");
            }


            bool createdByUser = groupToUpdate.CreatedById == firebaseUserId;
            bool isAdmin = groupMember?.IsAdmin == true;

            if (!createdByUser && !isAdmin)
            {
                return StatusCode(403, "Forbidden: You must be an admin of the group to update it");
            }


            // Update the group
            groupToUpdate.Name = group.Name ?? groupToUpdate.Name;
            groupToUpdate.Description = group.Description ?? groupToUpdate.Description;
            groupToUpdate.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            return Ok("Group updated");
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

        using (var batchTransaction = _context.Database.BeginTransaction())
        {
            try
            {
                // Find group
                Group? group = _context.Group.Find(groupId);
                if (group == null)
                {
                    return NotFound("Group not found");
                }

                // Find group member
                GroupMember? groupMember = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == groupId && gm.MemberId == firebaseUserId);
                if (groupMember == null)
                {
                    return StatusCode(403, "Forbidden: You must be a member of the group to leave it");
                }

                // Check if the user is the creator of the group
                if (group.CreatedById == firebaseUserId)
                {
                    return StatusCode(403, "You cannot leave a group you created. Delete the group instead");
                }

                // Check if the user is the last admin
                if (groupMember.IsAdmin)
                {
                    List<GroupMember> groupMembers = _context.GroupMember.Where(gm => gm.GroupId == groupId && gm.Status == GroupMemberStatus.Joined && gm.IsAdmin == true).ToList();
                    if (groupMembers.Count == 1)
                    {
                        return StatusCode(403, "You are the last admin of the group. Promote another member to admin before leaving");
                    }
                }

                // If a user is banned, they cannot "leave" the group (To ensure they can not rejoin)
                if (groupMember.Status == GroupMemberStatus.Banned)
                {
                    return StatusCode(403, "Forbidden: You are banned from the group");
                }

                if (groupMember.Status != GroupMemberStatus.Joined)
                {
                    return BadRequest("You are not in the group");
                }

                // Change the group member's status to "left"
                groupMember.Status = GroupMemberStatus.Left;
                _context.SaveChanges();

                // Remove user information from transaction details
                List<TransactionDetail> transactionDetails = _context.TransactionDetail.Where(td => td.GroupId == groupId && (td.PayerId == firebaseUserId || td.RecipientId == firebaseUserId)).ToList();

                foreach (var transactionDetail in transactionDetails)
                {
                    if (transactionDetail.PayerId == firebaseUserId)
                    {
                        transactionDetail.PayerId = null;
                    }
                    if (transactionDetail.RecipientId == firebaseUserId)
                    {
                        transactionDetail.RecipientId = null;
                    }
                }
                _context.SaveChanges();

                // Remove user information from transactions
                List<Transaction> transactions = _context.Transaction.Where(t => t.GroupId == groupId && (t.PayerId == firebaseUserId || t.CreatedById == firebaseUserId)).ToList();
                foreach (var transaction in transactions)
                {
                    if (transaction.PayerId == firebaseUserId)
                    {
                        transaction.PayerId = null;
                    }
                    if (transaction.CreatedById == firebaseUserId)
                    {
                        transaction.CreatedById = null;
                    }

                }

                _context.SaveChanges();

                groupMember.IsAdmin = false;
                _context.SaveChanges();

                batchTransaction.Commit();

                return Ok("You have left the group");
            }
            catch (Exception e)
            {
                _logger.LogError("LeaveGroup() failed with exception: {0}", e);
                return StatusCode(500, $"Internal server error: {e.Message}");
            }
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
        using (var batchTransaction = _context.Database.BeginTransaction())
        {
            try
            {
                // Find the group
                Group? group = _context.Group.Find(groupId);
                if (group == null)
                {
                    return NotFound("Group not found");
                }
                if (_context.User.Find(userId) == null)
                {
                    return NotFound("User not found");
                }
                if (group.CreatedById == userId)
                {
                    return StatusCode(403, "You cannot remove the creator of the group");
                }

                bool isCreator = group.CreatedById == firebaseUserId;
                bool isAdminAndMember = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == groupId && gm.MemberId == firebaseUserId)?.IsAdmin == true &&
                                        _context.GroupMember.FirstOrDefault(gm => gm.GroupId == groupId && gm.MemberId == firebaseUserId)?.Status == GroupMemberStatus.Joined;
                bool removingSelf = firebaseUserId == userId;
                if (removingSelf)
                {
                    return StatusCode(403, "You cannot remove yourself from the group. Leave the group instead");
                }

                if (!isCreator && !isAdminAndMember && !removingSelf)
                {
                    return StatusCode(403, "You must be an admin to remove users from the group");
                }

                // Find the member to remove
                GroupMember? memberToRemove = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == groupId && gm.MemberId == userId);
                if (memberToRemove == null)
                {
                    return BadRequest("User not found in group");
                }
                // Check if user is in the group
                if (memberToRemove.Status != GroupMemberStatus.Joined)
                {
                    return BadRequest("User is not in the group");
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

                // Remove the member by changing their status to "kicked"
                memberToRemove.IsAdmin = false;
                memberToRemove.Status = GroupMemberStatus.Kicked;
                _context.SaveChanges();

                // remove user information from kicked user's transactions and details
                List<TransactionDetail> transactionDetails = _context.TransactionDetail.Where(td => td.GroupId == groupId && (td.PayerId == userId || td.RecipientId == userId)).ToList();
                foreach (var transactionDetail in transactionDetails)
                {
                    if (transactionDetail.PayerId == userId)
                    {
                        transactionDetail.PayerId = null;
                    }
                    if (transactionDetail.RecipientId == userId)
                    {
                        transactionDetail.RecipientId = null;
                    }
                }

                _context.SaveChanges();

                List<Transaction> transactions = _context.Transaction.Where(t => t.GroupId == groupId && (t.PayerId == userId || t.CreatedById == userId)).ToList();

                foreach (var transaction in transactions)
                {
                    if (transaction.PayerId == userId)
                    {
                        transaction.PayerId = null;
                    }
                    if (transaction.CreatedById == userId)
                    {
                        transaction.CreatedById = null;
                    }
                }

                _context.SaveChanges();

                batchTransaction.Commit();

                return Ok("Member removed from group");
            }
            catch (Exception e)
            {
                _logger.LogError("RemoveMember() failed with exception: {0}", e);
                return StatusCode(500, $"Internal server error: {e.Message}");
            }
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

        using (var batchTransaction = _context.Database.BeginTransaction())
        {
            try
            {
                Group? group = _context.Group.Find(groupId);
                if (group == null)
                {
                    return NotFound("Group not found");
                }

                // Check role of user
                GroupMember? groupMember = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == groupId && gm.MemberId == firebaseUserId);
                if (groupMember == null)
                {
                    return NotFound("User not found in group");
                }

                GroupMember? memberToChange = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == groupId && gm.MemberId == userId);
                if (memberToChange == null)
                {
                    return NotFound("User not found in group");
                }



                // Creator has global permissions
                bool isCreator = group.CreatedById == firebaseUserId;

                // ***************************** Separate logic for different user statuses *****************************
                if (groupMember.Status == GroupMemberStatus.Banned)
                {
                    return StatusCode(403, "You are banned from the group");
                }


                else if (groupMember.Status == GroupMemberStatus.Invited)
                {
                    // an invited user can only accept or decline their own invite
                    if (firebaseUserId != userId || firebaseUserId != memberToChange.MemberId)
                    {
                        return StatusCode(403, "You can not change the status of another user");
                    }
                    if (newStatus != GroupMemberStatus.Joined && newStatus != GroupMemberStatus.Declined)
                    {
                        return StatusCode(403, "You can only accept or decline an invite");
                    }
                    groupMember.Status = newStatus;
                    groupMember.UpdatedAt = DateTime.UtcNow;
                    _context.SaveChanges();
                    batchTransaction.Commit();
                    return Ok("Member status changed");
                }

                else if (groupMember.Status == GroupMemberStatus.Joined)
                {
                    if (!groupMember.IsAdmin && !isCreator)
                    {
                        if (!(memberToChange.Status == GroupMemberStatus.Invited && newStatus == GroupMemberStatus.Kicked && memberToChange.InvitedById == firebaseUserId))
                        {
                            return StatusCode(403, "You must be an admin of the group to update it");
                        }
                    }
                    if (group.CreatedById == userId)
                    {
                        return StatusCode(403, "You cannot change the status of the creator of the group");
                    }
                    if (groupMember.MemberId == userId)
                    {
                        return StatusCode(403, "You cannot change your own status");
                    }

                    if (memberToChange.IsAdmin && !isCreator)
                    {
                        return StatusCode(403, "You cannot change the status of an admin");
                    }



                    // ********************* Logic for memberToChange statuses *********************
                    if (memberToChange.Status == GroupMemberStatus.Declined)
                    {
                        return StatusCode(403, "You cannot change the status of a user who is not in the group");
                    }
                    // ADD LOGIC TO REVOKE AN INVITE
                    else if (memberToChange.Status == GroupMemberStatus.Invited)
                    {
                        if (newStatus != GroupMemberStatus.Kicked)
                        {

                            return StatusCode(403, "You cannot change the status of a user who is not in the group");
                        }
                    }
                    else if (memberToChange.Status == GroupMemberStatus.Kicked)
                    {
                        return StatusCode(403, "You cannot change the status of a user who is not in the group");
                    }
                    else if (memberToChange.Status == GroupMemberStatus.Left)
                    {
                        return StatusCode(403, "You cannot change the status of a user who is not in the group");
                    }
                    else if (memberToChange.Status == GroupMemberStatus.Banned)
                    {
                        if (newStatus != GroupMemberStatus.Kicked)
                        {
                            return StatusCode(403, "you can only unban this user");
                        }
                    }
                    else if (memberToChange.Status == GroupMemberStatus.Joined)
                    {
                        if (newStatus != GroupMemberStatus.Kicked && newStatus != GroupMemberStatus.Banned)
                        {
                            return StatusCode(403, "You can only kick or ban this user");
                        }
                    }
                    else
                    {
                        return StatusCode(403, "You cannot change the status of a user who is not in the group");
                    }


                    memberToChange.Status = newStatus;
                    _context.SaveChanges();

                    memberToChange.UpdatedAt = DateTime.UtcNow;
                    _context.SaveChanges();

                    if (newStatus == GroupMemberStatus.Banned || newStatus == GroupMemberStatus.Kicked)
                    {
                        memberToChange.IsAdmin = false;
                        _context.SaveChanges();
                    }
                }

                batchTransaction.Commit();

                return Ok("Member status changed");
            }
            catch (Exception e)
            {
                _logger.LogError("ChangeMemberStatus() failed with exception: {0}", e);
                return StatusCode(500, $"Internal server error: {e.Message}");
            }
        }
    }

    /*****************************************************************************************************************************
    Promote a member to admin
    api/v1/groups/{groupId}/promote/{userId} [PUT]
    *****************************************************************************************************************************/
    [HttpPut("{groupId}/promote/{userId}", Name = "PromoteMemberToAdmin")]
    public ActionResult PromoteMemberToAdmin(int groupId, string userId)
    {
        _logger.LogInformation("PromoteMemberToAdmin() called");
        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;
        if (firebaseUserId == null)
        {
            return StatusCode(403, "Forbidden");
        }

        try
        {
            // Find the creator of the group
            Group? group = _context.Group.Find(groupId);
            if (group == null)
            {
                return NotFound("Group not found");
            }

            User? creator = _context.User.Find(group.CreatedById);
            if (creator == null)
            {
                return NotFound("Creator not found");
            }

            // Find the promoter
            GroupMember? promoter = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == groupId && gm.MemberId == firebaseUserId);
            if (promoter == null || promoter.Status != GroupMemberStatus.Joined)
            {
                return StatusCode(403, "You must be a member of the group to promote a member to admin");
            }
            if (promoter.MemberId != creator.Id && !promoter.IsAdmin)
            {
                return StatusCode(403, "You must be an admin to promote a member to admin");
            }

            // Find the member to promote
            GroupMember? memberToPromote = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == groupId && gm.MemberId == userId);
            if (memberToPromote == null)
            {
                return NotFound("User not found in group");
            }
            if (memberToPromote.Status != GroupMemberStatus.Joined)
            {
                return BadRequest("User is not in the group");
            }

            // Check if the user is the creator of the group
            if (group.CreatedById == userId)
            {
                return StatusCode(403, "You cannot edit the creator of the group");
            }

            // Promote the member
            memberToPromote.IsAdmin = true;

            _context.SaveChanges();

            return Ok("Member promoted to admin");
        }
        catch (Exception e)
        {
            _logger.LogError("PromoteMemberToAdmin() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

    /*****************************************************************************************************************************
    Demote an admin to member
    api/v1/groups/{groupId}/demote/{userId} [PUT]
    *****************************************************************************************************************************/
    [HttpPut("{groupId}/demote/{userId}", Name = "DemoteAdminToMember")]
    public ActionResult DemoteAdminToMember(int groupId, string userId)
    {
        _logger.LogInformation("DemoteAdminToMember() called");
        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;
        if (firebaseUserId == null)
        {
            return StatusCode(403, "Forbidden");
        }

        try
        {
            // Find the creator of the group
            Group? group = _context.Group.Find(groupId);
            if (group == null)
            {
                return NotFound("Group not found");
            }

            User? creator = _context.User.Find(group.CreatedById);
            if (creator == null)
            {
                return NotFound("Creator not found");
            }

            // Find the demoter
            GroupMember? demoter = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == groupId && gm.MemberId == firebaseUserId);
            if (demoter == null || demoter.MemberId != group.CreatedById)
            {
                return StatusCode(403, "You must be the creator of the group to demote an admin to member");
            }




            // Find the member to demote
            GroupMember? memberToDemote = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == groupId && gm.MemberId == userId);
            if (memberToDemote == null)
            {
                return NotFound("User not found in group");
            }
            if (memberToDemote.Status != GroupMemberStatus.Joined)
            {
                return BadRequest("User not found in group");
            }

            // Check if the user is the creator of the group
            if (group.CreatedById == userId)
            {
                return StatusCode(403, "You cannot edit the creator of the group");
            }

            if (!memberToDemote.IsAdmin)
            {
                return BadRequest("User is already a member");
            }

            // Demote the member
            memberToDemote.IsAdmin = false;

            _context.SaveChanges();

            return Ok("Admin demoted to member");
        }
        catch (Exception e)
        {
            _logger.LogError("DemoteAdminToMember() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

    /*****************************************************************************************************************************
    Transfer ownership of a group
    api/v1/groups/{groupId}/transferownership/{userId} [PUT]
    *****************************************************************************************************************************/
    [HttpPut("{groupId}/transferownership/{userId}", Name = "TransferOwnership")]
    public ActionResult TransferOwnership(int groupId, string userId)
    {
        _logger.LogInformation("TransferOwnership() called");
        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;
        if (firebaseUserId == null)
        {
            return StatusCode(403, "Forbidden");
        }
        using (var batchTransaction = _context.Database.BeginTransaction())
        {
            try
            {
                var group = _context.Group.Find(groupId);
                if (group == null)
                {
                    return NotFound("Group not found");
                }

                var currentOwner = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == groupId && gm.MemberId == group.CreatedById);
                if (currentOwner == null || currentOwner.Status != GroupMemberStatus.Joined)
                {
                    return NotFound("Current owner not found");
                }

                var newOwner = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == groupId && gm.MemberId == userId);
                if (newOwner == null || newOwner.Status != GroupMemberStatus.Joined)
                {
                    return NotFound("User is not in the group");
                }

                if (group.CreatedById != firebaseUserId)
                {
                    return StatusCode(403, "You must be the creator of the group to transfer ownership");
                }


                if (group.CreatedById == userId)
                {
                    return StatusCode(403, "User is already the owner of the group");
                }

                // Assign the new owner
                group.CreatedById = userId;
                _context.SaveChanges();

                newOwner.IsAdmin = true;
                _context.SaveChanges();

                batchTransaction.Commit();

                return Ok("Ownership transferred");
            }
            catch (Exception e)
            {
                _logger.LogError("TransferOwnership() failed with exception: {0}", e);
                return StatusCode(500, $"Internal server error: {e.Message}");
            }
        }



    }

}