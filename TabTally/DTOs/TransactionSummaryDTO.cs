public class TransactionSummaryDTO
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string? CreatedById { get; set; }

    public UserSummaryDTO? CreatedBy { get; set; }

    public string? PayerId { get; set; }

    public UserSummaryDTO? Payer { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int GroupId { get; set; }
    public ICollection<TransactionDetailsSummaryDTO> TransactionDetails { get; set; }
}