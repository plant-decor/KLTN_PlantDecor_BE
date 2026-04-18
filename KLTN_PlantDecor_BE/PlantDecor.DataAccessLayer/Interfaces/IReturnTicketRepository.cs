using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IReturnTicketRepository : IGenericRepository<ReturnTicket>
    {
        Task<List<ReturnTicket>> GetByCustomerIdWithDetailsAsync(int customerId);
    }
}
