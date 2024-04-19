public class CreateTransactionRequest
{
    public Transaction Transaction { get; set; }
    public List<TransactionDetailsPartial> TransactionDetailsPartial { get; set; }
}