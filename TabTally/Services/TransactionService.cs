public class TransactionService
{
    public bool TransactionTotalEqualsDetails(Transaction transaction, ICollection<TransactionDetailsPartial> transactionDetails)
    {
        decimal total = 0;
        foreach (TransactionDetailsPartial transactionDetail in transactionDetails)
        {
            total += transactionDetail.Amount;
        }
        return total == transaction.Amount;
    }

    public bool DetailsAndTransactionPayersMatch(Transaction transaction, ICollection<TransactionDetailsPartial> transactionDetails)
    {
        foreach (TransactionDetailsPartial transactionDetail in transactionDetails)
        {
            if (transactionDetail.PayerId != transaction.PayerId)
            {
                return false;
            }
        }
        return true;
    }

    public bool DetailsAndTransactionsGroupsMatch(Transaction transaction, ICollection<TransactionDetailsPartial> transactionDetails)
    {
        foreach (TransactionDetailsPartial transactionDetail in transactionDetails)
        {
            if (transactionDetail.GroupId != transaction.GroupId)
            {
                return false;
            }
        }
        return true;
    }
}