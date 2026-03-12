using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces.Payment;
using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Repositories.Payment
{
    public class TransactionRepository : RepositoryBase<Transaction>, ITransactionRepository
    {
        public TransactionRepository(ImateDbContext context) : base(context)
        {
        }
    }
}
