namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface ITokenCleanupService
    {
        Task CleanupRevokedTokensAsync();
    }
}
