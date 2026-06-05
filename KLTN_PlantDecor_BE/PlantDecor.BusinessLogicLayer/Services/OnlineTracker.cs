using PlantDecor.BusinessLogicLayer.Interfaces;
using System.Collections.Concurrent;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class OnlineTracker : IOnlineTracker
    {
        private readonly ConcurrentDictionary<int, HashSet<string>> _connections = new();
        private readonly object _lock = new();

        public void UserConnected(int userId, string connectionId)
        {
            lock (_lock)
            {
                if (!_connections.TryGetValue(userId, out var connections))
                {
                    connections = new HashSet<string>();
                    _connections[userId] = connections;
                }
                connections.Add(connectionId);
            }
        }

        public bool UserDisconnected(int userId, string connectionId)
        {
            lock (_lock)
            {
                if (!_connections.TryGetValue(userId, out var connections))
                    return false;

                connections.Remove(connectionId);

                if (connections.Count == 0)
                {
                    _connections.TryRemove(userId, out _);
                    return true;
                }

                return false;
            }
        }

        public bool IsOnline(int userId) => _connections.ContainsKey(userId);
    }
}
