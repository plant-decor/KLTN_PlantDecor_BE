namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface ICareReminderBackgroundJobService
    {
        Task ProcessTodayCareRemindersAsync();
    }
}
