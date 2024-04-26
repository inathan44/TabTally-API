public class CreateTransactionRequest
{
    public Transaction Transaction { get; set; }
    public List<ITransactionDetailsPartial> TransactionDetailsPartial { get; set; }
}