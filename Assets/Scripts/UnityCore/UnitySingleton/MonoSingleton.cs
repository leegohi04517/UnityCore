using UnityEngine;

namespace UnityCore.UnitySingleton
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>, new()
    {
        protected static T instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    var instanceName = typeof(T).Name;
                    var instanceGO = new GameObject(instanceName);
                    instance = instanceGO.AddComponent<T>();
                    DontDestroyOnLoad(instanceGO);
                }

                return instance;
            }
        }
    }
}