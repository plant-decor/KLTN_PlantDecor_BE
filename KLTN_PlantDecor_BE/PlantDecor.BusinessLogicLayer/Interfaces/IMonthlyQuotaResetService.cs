namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IMonthlyQuotaResetService
    {
        Task ResetMonthlyQuotaAsync();
        Task GrantMonthlyFreeQuotaAsync(int userId);
    }
}
