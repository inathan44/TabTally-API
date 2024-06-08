using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Splyt.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]

public class TransactionsController : ControllerBase
{
    private readonly ILogger<TransactionsController> _logger;
    private readonly SplytContext _context;
    private readonly TransactionService _transactionService;

    public TransactionsController(SplytContext context, ILogger<TransactionsController> logger, TransactionService transactionService)
    {
        _context = context;
        _logger = logger;
        _transactionService = transactionService;
    }

    /**********************************************************************************************************************
    // /api/v1/transactions [GET] - Gets all transactions
    // DEVELOPMENT AND TESTING ONLY
    **********************************************************************************************************************/
    [HttpGet(Name = "GetTransactions")]
    public ActionResult<ICollection<Transaction>> GetTransactions()
    {
        if (Environment.GetEnvironmentVariable("ENVIRONMENT") != "development")
        {
            return StatusCode(403, "Forbidden");
        }
        try
        {
            _logger.LogInformation("GetTransactions() called");

            {
                var transactions = _context.Transaction.Include(t => t.TransactionDetails).ToList();
                return Ok(transactions);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("GetTransactions() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

    /**********************************************************************************************************************
    // /api/v1/transactions/details/ [GET] - Gets all transaction details
    // DEVELOPMENT AND TESTING ONLY
    **********************************************************************************************************************/
    [HttpGet("details", Name = "GetTransactionDetails")]
    public ActionResult<ICollection<TransactionDetail>> GetTransactionDetails()
    {
        if (Environment.GetEnvironmentVariable("ENVIRONMENT") != "development")
        {
            return StatusCode(403, "Forbidden");
        }
        try
        {
            _logger.LogInformation("GetTransactionDetails() called");

            {
                var transactionDetails = _context.TransactionDetail.ToList();
                return Ok(transactionDetails);
            }
        }
        catch (Exception e)
        {
            _logger.LogError("GetTransactionDetails() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

    /**********************************************************************************************************************
    // /api/v1/transactions/{id} [GET] - Gets a specific transaction 
    ***********************************************************************************************************************/
    [HttpGet("{id}", Name = "GetTransaction")]
    public ActionResult<GetTransactionResponseDTO> GetTransaction(int id)
    {
        _logger.LogInformation("GetTransaction() called");
        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;
        if (firebaseUserId == null)
        {
            return StatusCode(403, "Must be logged in to view transaction");
        }
        try
        {
            var transaction = _context.Transaction
                .Include(t => t.TransactionDetails)
                    .ThenInclude(td => td.Recipient)
                .Include(t => t.Group)
                    .ThenInclude(g => g.CreatedBy)
                .FirstOrDefault(t => t.Id == id);
            if (transaction == null)
            {
                return NotFound("Transaction not found");
            }
            // must be in the group or be the creator of the transaction to view it
            var groupMember = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == transaction.GroupId && gm.MemberId == firebaseUserId);
            if (groupMember == null || groupMember.Status != GroupMemberStatus.Joined)
            {
                return StatusCode(403, "Must be in the group to view transaction");
            }

            // Shape the response
            var detailsResponse = new List<TransactionDetailsSummaryDTO>();
            foreach (var transactionDetail in transaction.TransactionDetails)
            {
                detailsResponse.Add(new TransactionDetailsSummaryDTO
                {
                    Id = transactionDetail.Id,
                    TransactionId = transactionDetail.TransactionId,
                    RecipientId = transactionDetail.RecipientId,
                    Recipient = transactionDetail.Recipient == null ? null : new UserSummaryDTO
                    {
                        Id = transactionDetail.Recipient.Id,
                        Username = transactionDetail.Recipient.Username,
                        FirstName = transactionDetail.Recipient.FirstName,
                        LastName = transactionDetail.Recipient.LastName,
                        CreatedAt = transactionDetail.Recipient.CreatedAt,
                        UpdatedAt = transactionDetail.Recipient.UpdatedAt,
                    },
                    Amount = transactionDetail.Amount,

                });

            }


            GetTransactionResponseDTO response = new GetTransactionResponseDTO
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                CreatedById = transaction.CreatedById,
                CreatedBy = transaction.CreatedBy == null ? null : new UserSummaryDTO
                {
                    Id = transaction.CreatedBy.Id,
                    Username = transaction.CreatedBy.Username,
                    FirstName = transaction.CreatedBy.FirstName,
                    LastName = transaction.CreatedBy.LastName,
                    CreatedAt = transaction.CreatedBy.CreatedAt,
                    UpdatedAt = transaction.CreatedBy.UpdatedAt,

                },
                PayerId = transaction.PayerId,
                Payer = transaction.Payer == null ? null : new UserSummaryDTO
                {
                    Id = transaction.Payer.Id,
                    Username = transaction.Payer.Username,
                    FirstName = transaction.Payer.FirstName,
                    LastName = transaction.Payer.LastName,
                    CreatedAt = transaction.Payer.CreatedAt,
                    UpdatedAt = transaction.Payer.UpdatedAt,
                },
                Description = transaction.Description,
                CreatedAt = transaction.CreatedAt,
                UpdatedAt = transaction.UpdatedAt,
                GroupId = transaction.GroupId,
                TransactionDetails = detailsResponse,
                Group = transaction.Group == null ? null : new GroupSummaryDTO
                {
                    Id = transaction.Group.Id,
                    Name = transaction.Group.Name,
                    Description = transaction.Group.Description,
                    CreatedById = transaction.Group.CreatedById,
                    CreatedBy = transaction.Group.CreatedBy == null ? new UserSummaryDTO() : new UserSummaryDTO
                    {
                        Id = transaction.Group.CreatedBy.Id,
                        Username = transaction.Group.CreatedBy.Username,
                        FirstName = transaction.Group.CreatedBy.FirstName,
                        LastName = transaction.Group.CreatedBy.LastName,
                        CreatedAt = transaction.Group.CreatedBy.CreatedAt,
                        UpdatedAt = transaction.Group.CreatedBy.UpdatedAt,
                    },
                    CreatedAt = transaction.Group.CreatedAt,
                    UpdatedAt = transaction.Group.UpdatedAt,
                },
            };

            return response;

        }
        catch (Exception e)
        {
            _logger.LogError("GetTransaction() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }

    }

    /**********************************************************************************************************************
        // /api/v1/Transactions/add [POST]
        // NEEDED: add peer to peer transaction capability
    **********************************************************************************************************************/
    [HttpPost("add", Name = "CreateTransaction")]
    public ActionResult<TransactionSummaryDTO> CreateTransaction([FromBody] CreateTransactionRequest transactionRequest)
    {
        ICollection<string> invalidProperties = new List<string>();

        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;
        if (firebaseUserId == null)
        {
            return StatusCode(403, "Must be logged in to create a transaction");
        }

        if (transactionRequest.GroupId == 0)
        {
            return BadRequest("Group ID is required");
        }
        if (transactionRequest.PayerId == null)
        {
            return BadRequest("Payer ID is required");
        }
        if (transactionRequest.Amount == 0.0M)
        {
            return BadRequest("Amount is required");
        }
        if (transactionRequest.TransactionDetails == null || transactionRequest.TransactionDetails.Count == 0)
        {
            return BadRequest("Transaction details are required");
        }


        using (var batchTransaction = _context.Database.BeginTransaction())
        {
            try
            {
                Group? group = _context.Group.Find(transactionRequest.GroupId);
                if (group == null)
                {
                    return NotFound("Group not found");
                }


                // Check if user is in the group
                GroupMember? groupMember = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == transactionRequest.GroupId && gm.MemberId == firebaseUserId);
                if (groupMember == null || groupMember.Status != GroupMemberStatus.Joined)
                {
                    return StatusCode(403, "Must be in the group to create a transaction");
                }

                // Check if payer is in the group
                GroupMember? payer = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == transactionRequest.GroupId && gm.MemberId == transactionRequest.PayerId);
                if (payer == null || payer.Status != GroupMemberStatus.Joined)
                {
                    return NotFound("Payer is not in this group");
                }

                // Check if each recipient is in the group
                foreach (var transactionDetail in transactionRequest.TransactionDetails)
                {
                    User? recipient = _context.User.Find(transactionDetail.RecipientId);
                    if (recipient == null)
                    {
                        return NotFound("user not found");
                    }
                    GroupMember? recipientGroupMember = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == transactionRequest.GroupId && gm.MemberId == recipient.Id);
                    if (recipientGroupMember == null || recipientGroupMember.Status != GroupMemberStatus.Joined)
                    {
                        return StatusCode(404, "One or more recipients are not in this group");
                    }
                }

                if (!_transactionService.TransactionTotalEqualsDetails(transactionRequest.Amount, transactionRequest.TransactionDetails.Select(td => td.Amount)))
                {
                    return BadRequest("Transaction total does not match transaction details total");
                }

                if (_transactionService.IsRepaymentTransaction(transactionRequest.Amount, transactionRequest.TransactionDetails.Select(td => td.Amount)))
                {
                    if (transactionRequest.TransactionDetails.Count != 1)
                    {
                        return BadRequest("Repayment transactions must have exactly one recipient");
                    }
                    if (transactionRequest.PayerId != firebaseUserId)
                    {
                        return StatusCode(403, "Cannot create a repayment for another user");
                    }
                }




                Transaction newTransaction = new Transaction
                {
                    CreatedById = firebaseUserId,
                    PayerId = transactionRequest.PayerId,
                    Amount = transactionRequest.Amount,
                    Description = transactionRequest.Description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    GroupId = transactionRequest.GroupId,
                };

                if (!TryValidateModel(newTransaction))
                {
                    return BadRequest(ModelState);
                }

                _context.Transaction.Add(newTransaction);
                _context.SaveChanges();

                List<TransactionDetailsSummaryDTO> detailsResponse = new List<TransactionDetailsSummaryDTO>();

                foreach (var transactionDetail in transactionRequest.TransactionDetails)
                {
                    TransactionDetail newTransactionDetail = new TransactionDetail
                    {
                        TransactionId = newTransaction.Id,
                        RecipientId = transactionDetail.RecipientId,
                        GroupId = newTransaction.GroupId,
                        Amount = transactionDetail.Amount
                    };
                    _context.TransactionDetail.Add(newTransactionDetail);

                    detailsResponse.Add(new TransactionDetailsSummaryDTO
                    {
                        Id = newTransactionDetail.Id,
                        TransactionId = newTransactionDetail.TransactionId,
                        RecipientId = newTransactionDetail.RecipientId,
                        Recipient = new UserSummaryDTO
                        {
                            Id = newTransactionDetail.Recipient.Id,
                            Username = newTransactionDetail.Recipient.Username,
                            FirstName = newTransactionDetail.Recipient.FirstName,
                            LastName = newTransactionDetail.Recipient.LastName,
                            CreatedAt = newTransactionDetail.Recipient.CreatedAt,
                            UpdatedAt = newTransactionDetail.Recipient.UpdatedAt,
                        },
                        Amount = newTransactionDetail.Amount,

                    });
                }

                _context.SaveChanges();
                batchTransaction.Commit();



                // shape the response
                TransactionSummaryDTO response = new TransactionSummaryDTO
                {
                    Id = newTransaction.Id,
                    Amount = newTransaction.Amount,
                    CreatedById = newTransaction.CreatedById,
                    CreatedBy = newTransaction.CreatedBy == null ? null : new UserSummaryDTO
                    {
                        Id = newTransaction.CreatedBy.Id,
                        Username = newTransaction.CreatedBy.Username,
                        FirstName = newTransaction.CreatedBy.FirstName,
                        LastName = newTransaction.CreatedBy.LastName,
                        CreatedAt = newTransaction.CreatedBy.CreatedAt,
                        UpdatedAt = newTransaction.CreatedBy.UpdatedAt,

                    },
                    PayerId = newTransaction.PayerId,
                    Payer = newTransaction.Payer == null ? null : new UserSummaryDTO
                    {
                        Id = newTransaction.Payer.Id,
                        Username = newTransaction.Payer.Username,
                        FirstName = newTransaction.Payer.FirstName,
                        LastName = newTransaction.Payer.LastName,
                        CreatedAt = newTransaction.Payer.CreatedAt,
                        UpdatedAt = newTransaction.Payer.UpdatedAt,
                    },
                    Description = newTransaction.Description,
                    CreatedAt = newTransaction.CreatedAt,
                    UpdatedAt = newTransaction.UpdatedAt,
                    GroupId = newTransaction.GroupId,
                    TransactionDetails = detailsResponse
                };

                return CreatedAtRoute("GetTransaction", new { id = newTransaction.Id }, response);

            }
            catch (Exception e)
            {
                _logger.LogError("CreateTransaction() failed with exception: {0}", e);
                batchTransaction.Rollback();
                return StatusCode(500, $"Internal server error: {e.Message}");
            }
        }
    }

    /**********************************************************************************************************************
    // /api/v1/Transactions/{id}/delete [DELETE]
    **********************************************************************************************************************/
    [HttpDelete("{id}/delete", Name = "DeleteTransaction")]
    public async Task<IActionResult> DeleteTransaction(int id)
    {
        _logger.LogInformation("DeleteTransaction() called");
        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;
        if (firebaseUserId == null)
        {
            return StatusCode(403, "Must be logged in to delete a transaction");
        }
        try
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                var transactionToDelete = _context.Transaction.Find(id);
                if (transactionToDelete == null)
                {
                    return NotFound("Transaction not found");
                }

                // Check if user is admin of the group or the creator of the transaction
                var group = _context.Group.Find(transactionToDelete.GroupId);
                if (group == null)
                {
                    return NotFound("Group not found");
                }
                var groupMember = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == group.Id && gm.MemberId == firebaseUserId);
                if (groupMember == null || groupMember.Status != GroupMemberStatus.Joined)
                {
                    return StatusCode(403, "Must be in the group to delete transaction");
                }

                if (transactionToDelete.CreatedById != firebaseUserId && groupMember.IsAdmin == false && transactionToDelete.PayerId != firebaseUserId)
                {
                    return StatusCode(403, "Must be the payer, an admin, or the creator to delete transaction");
                }

                _context.Transaction.Remove(transactionToDelete);
                await _context.SaveChangesAsync();

                transaction.Commit();

                return Ok("Transaction deleted");
            }
        }
        catch (Exception e)
        {
            _logger.LogError("DeleteTransaction() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

    /**********************************************************************************************************************
    // Edit transaction details
    // /api/v1/Transactions/{id}/details/edit [PUT]
    **********************************************************************************************************************/
    // [HttpPut("{id}/details/edit", Name = "EditTransactionDetails")]
    // public IActionResult EditTransactionDetails(int id, [FromBody] List<TransactionDetail> newTransactionDetails)
    // {
    //     _logger.LogInformation("EditTransactionDetails() called");
    //     var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;
    //     if (firebaseUserId == null)
    //     {
    //         return StatusCode(403, "Must be logged in to edit transaction details");
    //     }
    //     try
    //     {
    //         // Find transaction
    //         var transaction = _context.Transaction.Find(id);
    //         if (transaction == null)
    //         {
    //             return NotFound("Transaction not found: " + id);
    //         }

    //         // Update timestamp
    //         transaction.UpdatedAt = DateTime.UtcNow;

    //         // Find the group that the transaction is in
    //         var group = _context.Group.Find(transaction.GroupId);
    //         if (group == null)
    //         {
    //             return NotFound("Group not found");
    //         }

    //         // Check if user is a group admin
    //         var groupMember = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == group.Id && gm.MemberId == firebaseUserId);
    //         bool isAdmin = groupMember?.IsAdmin ?? false;

    //         // User must be the creator of the transaction or a group admin to edit it
    //         if (transaction.CreatedById != firebaseUserId && !isAdmin)
    //         {
    //             return StatusCode(403, "Must be the creator of the transaction to edit it");
    //         }

    //         // Check if transaction details are valid
    //         bool validDetails = _transactionService.TransactionTotalEqualsDetails(transaction, newTransactionDetails) && _transactionService.DetailsAndTransactionPayersMatch(transaction, newTransactionDetails) && _transactionService.DetailsAndTransactionsGroupsMatch(transaction, newTransactionDetails);
    //         if (!validDetails)
    //         {
    //             return BadRequest("Invalid transaction details");
    //         }

    //         /* Updated transaction details CAN NOT change the corresponding transaction ID
    //            deny request if put request attempts to update transaction ID */
    //         if (!_transactionService.DetailsAndTransactionIdsMatch(transaction, newTransactionDetails))
    //         {
    //             return BadRequest("Transaction details must match transaction ID");
    //         }

    //         // Delete old transaction details
    //         var oldTransactionDetails = _context.TransactionDetail.Where(td => td.TransactionId == id);
    //         _context.TransactionDetail.RemoveRange(oldTransactionDetails);



    //         // Add new transaction details
    //         foreach (var transactionDetail in newTransactionDetails)
    //         {
    //             _context.TransactionDetail.Add(transactionDetail);
    //         }

    //         // Update transaction
    //         _context.Transaction.Update(transaction);
    //         _context.SaveChanges();

    //         return NoContent();
    //     }
    //     catch (Exception e)
    //     {
    //         _logger.LogError("EditTransactionDetails() failed with exception: {0}", e);
    //         return StatusCode(500, $"Internal server error: {e.Message}");
    //     }
    // }

    /**********************************************************************************************************************
    // /api/v1/Transactions/{id}/edit [PUT]
    **********************************************************************************************************************/
    [HttpPut("{id}/edit", Name = "EditTransaction")]
    public IActionResult EditTransaction(int id, [FromBody] UpdateTransactionDTO updatedTransaction)
    {
        _logger.LogInformation("EditTransaction() called");
        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;
        if (firebaseUserId == null)
        {
            return StatusCode(403, "Must be logged in to edit a transaction");
        }
        try
        {
            using (var batchTransaction = _context.Database.BeginTransaction())
            {
                var transaction = _context.Transaction.Include(t => t.TransactionDetails).FirstOrDefault(t => t.Id == id);
                if (transaction == null)
                {
                    return NotFound("Transaction not found");
                }
                var updatingUser = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == transaction.GroupId && gm.MemberId == firebaseUserId);
                if (updatingUser == null || updatingUser.Status != GroupMemberStatus.Joined)
                {
                    return StatusCode(403, "Must be in the group to update transaction");
                }

                // If updating the amount
                if (updatedTransaction.Amount != null && updatedTransaction.Amount != transaction.Amount)
                {
                    if (updatedTransaction.Amount == 0)
                    {
                        return BadRequest("Transaction amount cannot be zero");
                    }

                    // transaction details can not be null because amount is changing, meaning details must be updated
                    if (updatedTransaction.TransactionDetails == null || updatedTransaction.TransactionDetails.Count == 0)
                    {
                        return BadRequest("Transaction details must be updated when the amount of the transaction changes");
                    }


                    // Ensure all member is transaction details are part of the group
                    foreach (var transactionDetail in updatedTransaction.TransactionDetails)
                    {
                        var recipientGroupMember = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == transaction.GroupId && gm.MemberId == transactionDetail.RecipientId);
                        if (recipientGroupMember == null || recipientGroupMember.Status != GroupMemberStatus.Joined)
                        {
                            return StatusCode(404, "One or more recipients are not in this group");
                        }
                    }

                    if (updatedTransaction.Amount != null && !_transactionService.TransactionTotalEqualsDetails(updatedTransaction.Amount!.Value, updatedTransaction.TransactionDetails.Select(td => td.Amount)))
                    {
                        return BadRequest("Transaction details do not match transaction amount");
                    }
                }

                // If its a repayment transaction, it can't be switched to a non-repayment transaction
                if (_transactionService.IsRepaymentTransaction(transaction.Amount, transaction.TransactionDetails.Select(td => td.Amount)))
                {
                    if (updatedTransaction.Amount > 0)
                    {
                        return BadRequest("Repayment transactions cannot be switched to non-repayment transactions");
                    }
                    if (updatedTransaction.TransactionDetails?.Count != 1)
                    {
                        return BadRequest("Repayment transactions must have exactly one recipient");
                    }
                }

                // if its a non-repayment transaction, it can't be switched to a repayment transaction
                if (!_transactionService.IsRepaymentTransaction(transaction.Amount, transaction.TransactionDetails.Select(td => td.Amount)))
                {
                    if (updatedTransaction.Amount < 0)
                    {
                        return BadRequest("Non-repayment transactions cannot be switched to repayment transactions");
                    }
                }

                // If updating the payer
                if (updatedTransaction.PayerId != null && updatedTransaction.PayerId != transaction.PayerId)
                {
                    // Check if the new payer is in the group
                    var newPayer = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == transaction.GroupId && gm.MemberId == updatedTransaction.PayerId);
                    if (newPayer == null || newPayer.Status != GroupMemberStatus.Joined)
                    {
                        return NotFound("New payer is not in this group");
                    }
                }

                // If updating the description
                if (updatedTransaction.Description != null && updatedTransaction.Description != transaction.Description)
                {
                    transaction.Description = updatedTransaction.Description;
                    _context.SaveChanges();
                }
                // If updating the amount
                if (updatedTransaction.Amount != null && updatedTransaction.Amount != transaction.Amount)
                {
                    transaction.Amount = updatedTransaction.Amount.Value;
                    _context.SaveChanges();
                }
                // If updating the payer
                if (updatedTransaction.PayerId != null && updatedTransaction.PayerId != transaction.PayerId)
                {
                    transaction.PayerId = updatedTransaction.PayerId;
                    _context.SaveChanges();
                }
                // if updating the transaction details
                if (updatedTransaction.TransactionDetails != null && updatedTransaction.TransactionDetails.Count > 0)
                {
                    // Delete old transaction details
                    var oldTransactionDetails = _context.TransactionDetail.Where(td => td.TransactionId == id);
                    _context.TransactionDetail.RemoveRange(oldTransactionDetails);

                    // Add new transaction details
                    foreach (var transactionDetail in updatedTransaction.TransactionDetails)
                    {
                        var newTransactionDetail = new TransactionDetail
                        {
                            TransactionId = transaction.Id,
                            RecipientId = transactionDetail.RecipientId,
                            GroupId = transaction.GroupId,
                            Amount = transactionDetail.Amount
                        };
                        _context.TransactionDetail.Add(newTransactionDetail);
                    }
                    _context.SaveChanges();
                }


                batchTransaction.Commit();
                return Ok("Transaction updated");
            }
        }

        catch (Exception e)
        {
            _logger.LogError("EditTransaction() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }
}