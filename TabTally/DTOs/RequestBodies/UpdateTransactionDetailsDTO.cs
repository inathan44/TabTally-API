using System.ComponentModel.DataAnnotations;

public class UpdateTransactionDetailsDTO
{
    public string RecipientId { get; set; }

    [Range(-999999999, 999999999)]
    public decimal Amount { get; set; }
}