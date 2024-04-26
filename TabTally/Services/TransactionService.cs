public class TransactionService
{
    public bool TransactionTotalEqualsDetails(Transaction transaction, IEnumerable<ITransactionDetailsPartial> transactionDetails)
    {
        decimal total = 0;
        foreach (ITransactionDetailsPartial transactionDetail in transactionDetails)
        {
            total += transactionDetail.Amount;
        }
        return total == transaction.Amount;
    }

    public bool DetailsAndTransactionPayersMatch(Transaction transaction, IEnumerable<ITransactionDetailsPartial> transactionDetails)
    {
        foreach (ITransactionDetailsPartial transactionDetail in transactionDetails)
        {
            if (transactionDetail.PayerId != transaction.PayerId)
            {
                return false;
            }
        }
        return true;
    }

    public bool DetailsAndTransactionsGroupsMatch(Transaction transaction, IEnumerable<ITransactionDetailsPartial> transactionDetails)
    {
        foreach (ITransactionDetailsPartial transactionDetail in transactionDetails)
        {
            if (transactionDetail.GroupId != transaction.GroupId)
            {
                return false;
            }
        }
        return true;
    }
    public bool DetailsAndTransactionIdsMatch(Transaction transaction, IEnumerable<TransactionDetails> transactionDetails)
    {
        foreach (TransactionDetails transactionDetail in transactionDetails)
        {
            if (transactionDetail.TransactionId != transaction.Id)
            {
                return false;
            }
        }
        return true;
    }
}