public class CreateTransactionRequest
{

    public string PayerId { get; set; }

    public decimal Amount { get; set; }
    public int GroupId { get; set; }

    public string Description { get; set; }
    public List<CreateTransactionTransactionDetailDTO> TransactionDetails { get; set; }
}

public class CreateTransactionTransactionDetailDTO
{
    public decimal Amount { get; set; }
    public string RecipientId { get; set; }
}