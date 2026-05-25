using PlantDecor.DataAccessLayer.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IConversationSummaryRepository : IGenericRepository<ConversationSummarySnapshot>
    {
        Task<ConversationSummarySnapshot?> GetByConversationIdAsync(int conversationId);
        Task<int> UpsertAsync(ConversationSummarySnapshot snapshot);
    }
}
