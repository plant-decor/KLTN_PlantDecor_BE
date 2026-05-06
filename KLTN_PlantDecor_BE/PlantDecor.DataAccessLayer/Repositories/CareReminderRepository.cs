using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;
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
                        .ThenInclude(plant => plant!.PlantImages)
                .Include(reminder => reminder.UserPlant)
                    .ThenInclude(userPlant => userPlant!.PlantInstance)
                        .ThenInclude(plantInstance => plantInstance!.Plant)
                            .ThenInclude(plant => plant!.PlantImages)
                .Include(reminder => reminder.UserPlant)
                    .ThenInclude(userPlant => userPlant!.PlantInstance)
                        .ThenInclude(plantInstance => plantInstance!.PlantImages)
                .Where(reminder => reminder.UserPlant != null && reminder.UserPlant.UserId == userId)
                .OrderBy(reminder => reminder.ReminderDate ?? reminder.ScheduledDate)
                .ThenByDescending(reminder => reminder.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<CareReminder>> GetByUserIdAndReminderDateAsync(int userId, DateOnly reminderDate)
        {
            return await _context.CareReminders
                .AsNoTracking()
                .Include(reminder => reminder.UserPlant)
                    .ThenInclude(userPlant => userPlant!.Plant)
                        .ThenInclude(plant => plant!.PlantImages)
                .Include(reminder => reminder.UserPlant)
                    .ThenInclude(userPlant => userPlant!.PlantInstance)
                        .ThenInclude(plantInstance => plantInstance!.Plant)
                            .ThenInclude(plant => plant!.PlantImages)
                .Include(reminder => reminder.UserPlant)
                    .ThenInclude(userPlant => userPlant!.PlantInstance)
                        .ThenInclude(plantInstance => plantInstance!.PlantImages)
                .Where(reminder => reminder.UserPlant != null
                    && reminder.UserPlant.UserId == userId
                    && reminder.ReminderDate == reminderDate)
                .OrderByDescending(reminder => reminder.CreatedAt)
                .ThenByDescending(reminder => reminder.Id)
                .ToListAsync();
        }

        public async Task<PaginatedResult<CareReminder>> GetByUserIdWithFiltersAsync(int userId, int? careType, Pagination pagination)
        {
            var query = _context.CareReminders
                .AsNoTracking()
                .Include(reminder => reminder.UserPlant)
                    .ThenInclude(userPlant => userPlant!.Plant)
                        .ThenInclude(plant => plant!.PlantImages)
                .Include(reminder => reminder.UserPlant)
                    .ThenInclude(userPlant => userPlant!.PlantInstance)
                        .ThenInclude(plantInstance => plantInstance!.Plant)
                            .ThenInclude(plant => plant!.PlantImages)
                .Include(reminder => reminder.UserPlant)
                    .ThenInclude(userPlant => userPlant!.PlantInstance)
                        .ThenInclude(plantInstance => plantInstance!.PlantImages)
                .Where(reminder => reminder.UserPlant != null && reminder.UserPlant.UserId == userId);

            if (careType.HasValue)
            {
                query = query.Where(reminder => reminder.CareType == careType);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(reminder => reminder.ReminderDate)
                .ThenByDescending(reminder => reminder.CreatedAt)
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<CareReminder>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<List<CareReminder>> GetByReminderDateWithUserPlantAsync(DateOnly reminderDate)
        {
            return await _context.CareReminders
                .Include(reminder => reminder.UserPlant)
                .Where(reminder => reminder.ReminderDate == reminderDate)
                .ToListAsync();
        }
    }
}
