public class TransactionDetailsSummaryDTO
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public string? PayerId { get; set; }
    public UserSummaryDTO? Payer { get; set; }

    public string? RecipientId { get; set; }

    public UserSummaryDTO? Recipient { get; set; }
    public decimal Amount { get; set; }
}