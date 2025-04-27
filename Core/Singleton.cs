using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    protected static Singleton<T> instance
    {
        get
        {
            if (!_instance)
            {
                T[] managers = FindObjectsByType(typeof(T), FindObjectsInactive.Include, FindObjectsSortMode.None) as T[];
                if (managers.Length != 0)
                {
                    if (managers.Length == 1)
                    {
                        _instance = managers[0];
                        return _instance;
                    }
                    else
                    {
                        Debug.LogError("You have more than one " + typeof(T).Name + " in the scene. You only need 1, it's a singleton!");
                        foreach (T manager in managers)
                        {
                            Destroy(manager.gameObject);
                        }
                    }
                }
                if (managers.Length <= 0)
                {
                    GameObject obj = new GameObject(typeof(T).Name, typeof(T));
                    _instance = obj.GetComponent<T>();
                }
            }
            return _instance;
        }
        set
        {
            _instance = value as T;
        }
    }

    private static T _instance;
}