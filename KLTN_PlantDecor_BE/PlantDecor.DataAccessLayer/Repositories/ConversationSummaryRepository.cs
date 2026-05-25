using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class ConversationSummaryRepository : GenericRepository<ConversationSummarySnapshot>, IConversationSummaryRepository
    {
        public ConversationSummaryRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<ConversationSummarySnapshot?> GetByConversationIdAsync(int conversationId)
        {
            return await _context.Set<ConversationSummarySnapshot>()
                .FirstOrDefaultAsync(s => s.ConversationId == conversationId);
        }

        public async Task<int> UpsertAsync(ConversationSummarySnapshot snapshot)
        {
            if (snapshot == null) return 0;

            var existing = await GetByConversationIdAsync(snapshot.ConversationId ?? 0);
            if (existing == null)
            {
                await _context.AddAsync(snapshot);
            }
            else
            {
                existing.Summary = snapshot.Summary;
                existing.KeyPointsJson = snapshot.KeyPointsJson;
                existing.NextActionsJson = snapshot.NextActionsJson;
                existing.StructuredFeaturesJson = snapshot.StructuredFeaturesJson;
                existing.SourceWindow = snapshot.SourceWindow;
                existing.TranscriptHash = snapshot.TranscriptHash;
                existing.GeneratedAt = snapshot.GeneratedAt;
                _context.Update(existing);
            }

            return await _context.SaveChangesAsync();
        }
    }
}
