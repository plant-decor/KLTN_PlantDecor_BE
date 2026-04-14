namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IServiceCareBackgroundJobService
    {
        Task GenerateServiceScheduleAsync(int serviceRegistrationId);
    }
}
