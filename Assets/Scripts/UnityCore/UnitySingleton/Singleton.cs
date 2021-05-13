namespace UnityCore.UnitySingleton
{
    public class Singleton<T> where T : new()
    {
        private static readonly object _lock = new object();
        private static T _instance;

        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    return _instance != null ? _instance : _instance = new T();
                }
            }
        }
    }
}