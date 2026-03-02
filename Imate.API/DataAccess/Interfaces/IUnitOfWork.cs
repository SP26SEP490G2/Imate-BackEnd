using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Interfaces
{
    public interface IAccountRepository : IRepositoryBase<Account>
    {
        // Add specific methods here
    }

    public interface IUnitOfWork
    {
        IAccountRepository Account { get; }
        Task SaveAsync();
    }
}
