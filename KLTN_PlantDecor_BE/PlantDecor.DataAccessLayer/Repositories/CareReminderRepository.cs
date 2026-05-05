using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class CareReminderRepository : GenericRepository<CareReminder>, ICareReminderRepository
    {
        public CareReminderRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<List<CareReminder>> GetAllWithDetailsAsync()
        {
            return await _context.CareReminders
                .AsNoTracking()
                .Include(reminder => reminder.UserPlant)
                    .ThenInclude(userPlant => userPlant!.Plant)
                .Include(reminder => reminder.UserPlant)
                    .ThenInclude(userPlant => userPlant!.PlantInstance)
                        .ThenInclude(plantInstance => plantInstance!.Plant)
                .OrderByDescending(reminder => reminder.CreatedAt)
                .ThenByDescending(reminder => reminder.Id)
                .ToListAsync();
        }

        public async Task<CareReminder?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.CareReminders
                .AsNoTracking()
                .Include(reminder => reminder.UserPlant)
                    .ThenInclude(userPlant => userPlant!.Plant)
                .Include(reminder => reminder.UserPlant)
                    .ThenInclude(userPlant => userPlant!.PlantInstance)
                        .ThenInclude(plantInstance => plantInstance!.Plant)
                .FirstOrDefaultAsync(reminder => reminder.Id == id);
        }

        public async Task<List<CareReminder>> GetByUserIdWithDetailsAsync(int userId)
        {
            return await _context.CareReminders
                .AsNoTracking()
                .Include(reminder => reminder.UserPlant)
                    .ThenInclude(userPlant => userPlant!.Plant)
                .Include(reminder => reminder.UserPlant)
                    .ThenInclude(userPlant => userPlant!.PlantInstance)
                        .ThenInclude(plantInstance => plantInstance!.Plant)
                .Where(reminder => reminder.UserPlant != null && reminder.UserPlant.UserId == userId)
                .OrderBy(reminder => reminder.ReminderDate ?? reminder.ScheduledDate)
                .ThenByDescending(reminder => reminder.CreatedAt)
                .ToListAsync();
        }
    }
}
