namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IOnlineTracker
    {
        void UserConnected(int userId, string connectionId);
        /// <returns>true if the user just went fully offline (last connection removed)</returns>
        bool UserDisconnected(int userId, string connectionId);
        bool IsOnline(int userId);
    }
}
