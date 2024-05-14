public class TransactionService
{
    public bool TransactionTotalEqualsDetails(decimal transactionAmount, IEnumerable<Decimal> transactionDetailsAmounts)
    {
        decimal total = 0;
        foreach (var transactionDetailsAmount in transactionDetailsAmounts)
        {
            total += transactionDetailsAmount;
        }
        return total == transactionAmount;
    }


}