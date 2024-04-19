using System.ComponentModel.DataAnnotations;

public class TransactionDetailsPartial
{
    public int PayerId { get; set; }
    public int RecipientId { get; set; }
    public int GroupId { get; set; }

    [Range(0, 999999999)]
    public decimal Amount { get; set; }
}