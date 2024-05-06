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
    // NEEDED: Only admins should be able to access this endpoint and pagination should be implemented
    **********************************************************************************************************************/
    [HttpGet(Name = "GetTransactions")]
    // RENAME THIS FUNCTION
    public ActionResult<ICollection<Transaction>> GetTransactions()
    {
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
    // /api/v1/transactions/{id} [GET] - Gets a specific transaction 
    // NEEDED: Admins and users who are part of the group should be able to access this endpoint
    ***********************************************************************************************************************/
    [HttpGet("{id}", Name = "GetTransaction")]
    public ActionResult<Transaction> GetTransaction(int id)
    {
        _logger.LogInformation("GetTransaction() called");
        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;
        try
        {
            if (firebaseUserId == null)
            {
                return StatusCode(403, "Must be logged in to view transaction");
            }
            var transaction = _context.Transaction
                .Include(t => t.TransactionDetails)
                .FirstOrDefault(t => t.Id == id);
            if (transaction == null)
            {
                return NotFound("Transaction not found: " + id);
            }
            // must be in the group or be the creator of the transaction to view it
            var groupMember = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == transaction.GroupId && gm.MemberId == firebaseUserId);
            if (groupMember == null && transaction.CreatedById != firebaseUserId)
            {
                return StatusCode(403, "Must be in the group or the creator of the transaction to view it");
            }

            return Ok(transaction);

        }
        catch (Exception e)
        {
            _logger.LogError("GetTransaction() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }

    }

    /**********************************************************************************************************************
        // /api/v1/Transactions/add [POST]
    **********************************************************************************************************************/
    [HttpPost("add", Name = "CreateTransaction")]
    public IActionResult CreateTransaction([FromBody] CreateTransactionRequest transactionRequest)
    {
        ICollection<string> invalidProperties = new List<string>();

        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;
        if (firebaseUserId == null)
        {
            return StatusCode(403, "Must be logged in to create a transaction");
        }
        try
        {
            if (transactionRequest == null || transactionRequest.Transaction == null || transactionRequest.TransactionDetailsPartial == null)
            {
                _logger.LogError("CreateTransaction() called with null request");
                return BadRequest("Must send a request");
            }

            _logger.LogInformation("CreateTransaction() called");

            using var dbContextTransaction = _context.Database.BeginTransaction();
            try
            {
                // If user is not in the group they are trying to create a transaction for, return 403
                var groupMember = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == transactionRequest.Transaction.GroupId && gm.MemberId == firebaseUserId);
                if (groupMember == null)
                {
                    return StatusCode(403, "Must be in the group to create a transaction");
                }


                // Check if amounts equal total from transaction
                if (!_transactionService.TransactionTotalEqualsDetails(transactionRequest.Transaction, transactionRequest.TransactionDetailsPartial))
                {
                    return BadRequest("Transaction total does not equal sum of transaction details");
                }


                var group = _context.Group.Find(transactionRequest.Transaction.GroupId);
                if (group == null)
                {
                    return NotFound($"group id does not exist");
                }
                var payer = _context.User.Find(transactionRequest.Transaction.PayerId);
                if (payer == null)
                {
                    return NotFound($"payer id does not exist");
                }
                var createdBy = _context.User.Find(transactionRequest.Transaction.CreatedBy);
                if (createdBy == null)
                {
                    return NotFound($"created by id does not exist");
                }

                // Check if payer ID from transaction details matches transaction payer ID
                if (!_transactionService.DetailsAndTransactionPayersMatch(transactionRequest.Transaction, transactionRequest.TransactionDetailsPartial))
                {
                    return BadRequest("Payer ID from transaction details does not match transaction payer ID");
                }

                // Check that the details match the transaction group
                if (!_transactionService.DetailsAndTransactionsGroupsMatch(transactionRequest.Transaction, transactionRequest.TransactionDetailsPartial))
                {
                    return BadRequest("Transaction group does not match transaction details group");
                }

                // Each user in transaction details must be in the group, else deny request
                foreach (var transactionDetailPartial in transactionRequest.TransactionDetailsPartial)
                {
                    var groupMemberDetail = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == transactionDetailPartial.GroupId && gm.MemberId == transactionDetailPartial.RecipientId);
                    if (groupMemberDetail == null)
                    {
                        return BadRequest("Recipient in transaction details must be in the group");
                    }
                }

                // Add created_at and updated_at timestamps
                var transactionWithTimestamps = new Transaction
                {
                    CreatedBy = transactionRequest.Transaction.CreatedBy,
                    PayerId = transactionRequest.Transaction.PayerId,
                    Amount = transactionRequest.Transaction.Amount,
                    Description = transactionRequest.Transaction.Description ?? null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    GroupId = transactionRequest.Transaction.GroupId
                };

                _context.Transaction.Add(transactionWithTimestamps);
                _context.SaveChanges();

                int transactionId = transactionWithTimestamps.Id; // Get the ID from transactionWithTimestamps

                foreach (var transactionDetailPartial in transactionRequest.TransactionDetailsPartial)
                {
                    var transactionDetail = new TransactionDetail
                    {
                        PayerId = transactionDetailPartial.PayerId,
                        RecipientId = transactionDetailPartial.RecipientId,
                        GroupId = transactionDetailPartial.GroupId,
                        Amount = transactionDetailPartial.Amount,
                        TransactionId = transactionId // Use the ID from transactionWithTimestamps
                    };

                    _context.TransactionDetail.Add(transactionDetail);

                    _context.SaveChanges();
                }

                dbContextTransaction.Commit();

                Transaction? savedTransaction = _context.Transaction
                    .Include(t => t.TransactionDetails)
                    .FirstOrDefault(t => t.Id == transactionWithTimestamps.Id);

                if (savedTransaction != null)
                {
                    return CreatedAtAction(nameof(GetTransaction), new { id = savedTransaction.Id }, savedTransaction);
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception e)
            {
                dbContextTransaction.Rollback();
                _logger.LogError("CreateTransaction() failed with exception: {0}", e);
                return StatusCode(500, $"Internal server error: {e.Message}");
            }


        }
        catch (Exception e)
        {
            _logger.LogError("CreateTransaction() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
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
                    return NotFound("Transaction not found: " + id);
                }

                // Check if user is admin of the group or the creator of the transaction
                var group = _context.Group.Find(transactionToDelete.GroupId);
                if (group == null)
                {
                    return NotFound("Group not found");
                }
                var groupMember = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == group.Id && gm.MemberId == firebaseUserId);
                if (groupMember == null)
                {
                    return StatusCode(403, "Must be in the group to delete a transaction");
                }

                if (transactionToDelete.CreatedById != firebaseUserId && groupMember.IsAdmin == false)
                {
                    return StatusCode(403, "Must be the creator of the transaction or a group admin to delete it");
                }

                _context.Transaction.Remove(transactionToDelete);
                await _context.SaveChangesAsync();

                transaction.Commit();

                return Ok(transactionToDelete);
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
    [HttpPut("{id}/details/edit", Name = "EditTransactionDetails")]
    public IActionResult EditTransactionDetails(int id, [FromBody] List<TransactionDetail> newTransactionDetails)
    {
        _logger.LogInformation("EditTransactionDetails() called");
        var firebaseUserId = HttpContext.Items["FirebaseUserId"] as string;
        if (firebaseUserId == null)
        {
            return StatusCode(403, "Must be logged in to edit transaction details");
        }
        try
        {
            // Find transaction
            var transaction = _context.Transaction.Find(id);
            if (transaction == null)
            {
                return NotFound("Transaction not found: " + id);
            }

            // Update timestamp
            transaction.UpdatedAt = DateTime.UtcNow;

            // Find the group that the transaction is in
            var group = _context.Group.Find(transaction.GroupId);
            if (group == null)
            {
                return NotFound("Group not found");
            }

            // Check if user is a group admin
            var groupMember = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == group.Id && gm.MemberId == firebaseUserId);
            bool isAdmin = groupMember?.IsAdmin ?? false;

            // User must be the creator of the transaction or a group admin to edit it
            if (transaction.CreatedById != firebaseUserId && !isAdmin)
            {
                return StatusCode(403, "Must be the creator of the transaction to edit it");
            }

            // Check if transaction details are valid
            bool validDetails = _transactionService.TransactionTotalEqualsDetails(transaction, newTransactionDetails) && _transactionService.DetailsAndTransactionPayersMatch(transaction, newTransactionDetails) && _transactionService.DetailsAndTransactionsGroupsMatch(transaction, newTransactionDetails);
            if (!validDetails)
            {
                return BadRequest("Invalid transaction details");
            }

            /* Updated transaction details CAN NOT change the corresponding transaction ID
               deny request if put request attempts to update transaction ID */
            if (!_transactionService.DetailsAndTransactionIdsMatch(transaction, newTransactionDetails))
            {
                return BadRequest("Transaction details must match transaction ID");
            }

            // Delete old transaction details
            var oldTransactionDetails = _context.TransactionDetail.Where(td => td.TransactionId == id);
            _context.TransactionDetail.RemoveRange(oldTransactionDetails);



            // Add new transaction details
            foreach (var transactionDetail in newTransactionDetails)
            {
                _context.TransactionDetail.Add(transactionDetail);
            }

            // Update transaction
            _context.Transaction.Update(transaction);
            _context.SaveChanges();

            return NoContent();
        }
        catch (Exception e)
        {
            _logger.LogError("EditTransactionDetails() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

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
                var transactionToEdit = _context.Transaction.Find(id);
                if (transactionToEdit == null)
                {
                    return NotFound("Transaction not found: " + id);
                }
                // Update timestamp
                transactionToEdit.UpdatedAt = DateTime.UtcNow;

                // Find the group that the transaction is in
                var group = _context.Group.Find(transactionToEdit.GroupId);
                if (group == null)
                {
                    return NotFound("Group not found");
                }

                // Check if user is a group admin or the creator of the transaction
                var groupMember = _context.GroupMember.FirstOrDefault(gm => gm.GroupId == group.Id && gm.MemberId == firebaseUserId);
                bool isAdmin = groupMember?.IsAdmin ?? false;
                bool isCreator = transactionToEdit.CreatedById == firebaseUserId;

                if (!isAdmin && !isCreator)
                {
                    return StatusCode(403, "Must be the creator of the transaction or a group admin to edit it");
                }

                // Check if transaction details are valid
                bool validDetails = updatedTransaction.TransactionDetails != null &&
                _transactionService.TransactionTotalEqualsDetails(transactionToEdit, (IEnumerable<ITransactionDetailsPartial>)updatedTransaction.TransactionDetails);

                if (!validDetails)
                {
                    return BadRequest("Invalid transaction details");
                }

                // create new transaction details
                var oldTransactionDetails = _context.TransactionDetail.Where(td => td.TransactionId == id);
                _context.TransactionDetail.RemoveRange(oldTransactionDetails);

                if (updatedTransaction.TransactionDetails != null)
                {
                    foreach (var transactionDetail in updatedTransaction.TransactionDetails)
                    {
                        TransactionDetail newDetail = new TransactionDetail
                        {
                            TransactionId = transactionToEdit.Id,
                            // Can not edit payer id here, this comes from the transaction itself so they always match
                            PayerId = transactionToEdit.PayerId,
                            RecipientId = transactionDetail.RecipientId,
                            // Can not edit group id
                            GroupId = transactionToEdit.GroupId,
                            Amount = transactionDetail.Amount,
                        };
                        _context.TransactionDetail.Add(newDetail);
                    }
                }


                // Update transaction

                if (updatedTransaction.Amount != null)
                {
                    transactionToEdit.Amount = (decimal)updatedTransaction.Amount;
                }
                if (updatedTransaction.Description != null)
                {
                    transactionToEdit.Description = updatedTransaction.Description;
                }
                if (updatedTransaction.PayerId != null)
                {
                    transactionToEdit.PayerId = updatedTransaction.PayerId;
                }

                _context.Transaction.Update(transactionToEdit);
                _context.SaveChanges();

                return NoContent();
            }
        }

        catch (Exception e)
        {
            _logger.LogError("EditTransaction() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }
}