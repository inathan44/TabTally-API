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

    // /api/v1/transactions [GET] - Gets all transactions
    // NEEDED: Only admins should be able to access this endpoint and pagination should be implemented
    [HttpGet(Name = "GetTransactions")]
    public ActionResult<ICollection<Transaction>> GetGroupTransactions()
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

    // /api/v1/transactions/{id} [GET] - Gets a specific transaction
    // NEEDED: Admins and users who are part of the group should be able to access this endpoint
    [HttpGet("{id}", Name = "GetTransaction")]
    public ActionResult<Transaction> GetTransaction(int id)
    {
        _logger.LogInformation("GetTransaction() called");


        var transaction = _context.Transaction
     //  .Include(t => t.Group)
     .Include(t => t.TransactionDetails)
     .FirstOrDefault(t => t.Id == id);
        if (transaction == null)
        {
            return NotFound("Transaction not found: " + id);
        }
        else
        {
            return Ok(transaction);
        }

    }

    // /api/v1/Transactions/add [POST]
    [HttpPost("add", Name = "CreateTransaction")]
    public IActionResult CreateTransaction([FromBody] CreateTransactionRequest transactionRequest)
    {
        ICollection<string> invalidProperties = new List<string>();


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
                _logger.LogInformation("Database connection successful");

                // Check if amounts equal total from transaction
                if (!_transactionService.TransactionTotalEqualsDetails(transactionRequest.Transaction, transactionRequest.TransactionDetailsPartial))
                {
                    return BadRequest("Transaction total does not equal sum of transaction details");
                }


                var group = _context.Group.Find(transactionRequest.Transaction.GroupId);
                if (group == null)
                {
                    // Change bad request to send a better error message
                    return NotFound($"group id does not exist: {transactionRequest.Transaction.GroupId}");
                }
                var payer = _context.User.Find(transactionRequest.Transaction.PayerId);
                if (payer == null)
                {
                    return NotFound($"payer id does not exist: {transactionRequest.Transaction.PayerId}");
                }
                var createdBy = _context.User.Find(transactionRequest.Transaction.CreatedBy);
                if (createdBy == null)
                {
                    return NotFound($"created by id does not exist: {transactionRequest.Transaction.CreatedBy}");
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
                    var transactionDetail = new TransactionDetails
                    {
                        PayerId = transactionDetailPartial.PayerId,
                        RecipientId = transactionDetailPartial.RecipientId,
                        GroupId = transactionDetailPartial.GroupId,
                        Amount = transactionDetailPartial.Amount,
                        TransactionId = transactionId // Use the ID from transactionWithTimestamps
                    };

                    _context.TransactionDetails.Add(transactionDetail);

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

    // /api/v1/Transactions/{id}/delete [DELETE]
    [HttpDelete("{id}/delete", Name = "DeleteTransaction")]
    // NEEDED: PASS IN A USER to make sure they are allowed to delete the transaction
    public IActionResult DeleteTransaction(int id)
    {
        _logger.LogInformation("DeleteTransaction() called");

        try
        {
            var transaction = _context.Transaction.Find(id);
            if (transaction == null)
            {
                return NotFound("Transaction not found: " + id);
            }

            _context.Transaction.Remove(transaction);
            _context.SaveChanges();

            return Ok(transaction);
        }
        catch (Exception e)
        {
            _logger.LogError("DeleteTransaction() failed with exception: {0}", e);
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

}