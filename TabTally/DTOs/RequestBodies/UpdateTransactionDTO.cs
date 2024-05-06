using System.ComponentModel.DataAnnotations;

public class UpdateTransactionDTO
{
    public string? PayerId { get; set; }

    [Range(0, 999999999)]
    public decimal? Amount { get; set; }

    public string? Description { get; set; }

    public List<UpdateTransactionDetailsDTO>? TransactionDetails { get; set; }
}