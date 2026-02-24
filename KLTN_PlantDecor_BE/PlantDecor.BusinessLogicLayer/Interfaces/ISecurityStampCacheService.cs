namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface ISecurityStampCacheService
    {
        Task<bool> ValidateSecurityStampAsync(int userId, string tokenStamp);
        Task InvalidateSecurityStampAsync(int userId);
        Task SetSecurityStampAsync(int userId, string stamp);

    }
}
