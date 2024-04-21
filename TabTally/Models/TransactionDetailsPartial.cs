using System.ComponentModel.DataAnnotations;

public class TransactionDetailsPartial
{
    public string PayerId { get; set; }
    public string RecipientId { get; set; }
    public int GroupId { get; set; }

    [Range(0, 999999999)]
    public decimal Amount { get; set; }
}