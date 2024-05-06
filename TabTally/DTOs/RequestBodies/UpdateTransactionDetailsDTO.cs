using System.ComponentModel.DataAnnotations;

public class UpdateTransactionDetailsDTO
{
    public string RecipientId { get; set; }

    [Range(0, 999999999)]
    public decimal Amount { get; set; }
}