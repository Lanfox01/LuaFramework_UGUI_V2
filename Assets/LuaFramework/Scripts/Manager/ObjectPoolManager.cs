using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

namespace LuaFramework {
    /// <summary>
    /// 对象池管理器，分普通类对象池+资源游戏对象池 ; 真正执行数据是在 GameObjectPool 中，本来相当于管理和分流
    /// </summary>
    public class ObjectPoolManager : Manager {
        private Transform m_PoolRootObject = null;
        private Dictionary<string, object> m_ObjectPools = new Dictionary<string, object>(); // 普通类池
        private Dictionary<string, GameObjectPool> m_GameObjectPools = new Dictionary<string, GameObjectPool>(); // 游戏对象池
        // 设置本脚本之下挂载一个对象 作为池子的根节点
        Transform PoolRootObject {
            get {
                if (m_PoolRootObject == null) {
                    var objectPool = new GameObject("ObjectPool");
                    objectPool.transform.SetParent(transform);
                    objectPool.transform.localScale = Vector3.one;
                    objectPool.transform.localPosition = Vector3.zero;
                    m_PoolRootObject = objectPool.transform;
                }
                return m_PoolRootObject;
            }
        }
        /// <summary>
        /// 主要针对的是这个游戏对象池， 一下子初始化多少个容量
        /// </summary>
        /// <param name="poolName"></param>
        /// <param name="initSize"></param>
        /// <param name="maxSize"></param>
        /// <param name="prefab"></param>
        /// <returns></returns>
        public GameObjectPool CreatePool(string poolName, int initSize, int maxSize, GameObject prefab) {
            var pool = new GameObjectPool(poolName, prefab, initSize, maxSize, PoolRootObject);
            m_GameObjectPools[poolName] = pool;
            return pool;
        }

        public GameObjectPool GetPool(string poolName) {
            if (m_GameObjectPools.ContainsKey(poolName)) {
                return m_GameObjectPools[poolName];
            }
            return null;
        }

        public GameObject Get(string poolName) {
            GameObject result = null;
            if (m_GameObjectPools.ContainsKey(poolName)) {
                GameObjectPool pool = m_GameObjectPools[poolName];
                result = pool.NextAvailableObject();
                if (result == null) {
                    Debug.LogWarning("No object available in pool. Consider setting fixedSize to false.: " + poolName);
                }
            } else {
                Debug.LogError("Invalid pool name specified: " + poolName);
            }
            return result;
        }

        public void Release(string poolName, GameObject go) {
            if (m_GameObjectPools.ContainsKey(poolName)) {
                GameObjectPool pool = m_GameObjectPools[poolName];
                pool.ReturnObjectToPool(poolName, go);
            } else {
                Debug.LogWarning("No pool available with name: " + poolName);
            }
        }
        // 上面是 游戏对象缓冲池  游戏对象缓冲池  一个池子里面可以有多个预先压入的实例化好的对象，也可单独压一个
        ///-----------------------------------------------------------------------------------------------
        // 下面是 一般类对象缓冲池   正常情况下智能有一个 类实例对象？？
        public ObjectPool<T> CreatePool<T>(UnityAction<T> actionOnGet, UnityAction<T> actionOnRelease) where T : class {
            var type = typeof(T); // 反射这个泛型类信息
            var pool = new ObjectPool<T>(actionOnGet, actionOnRelease);// 新建一个对象池，然后把两个事件代入绑定，分别是实例化一个实例对象的时候和示范
            m_ObjectPools[type.Name] = pool; // 同样 翻入字典 查询用， 本身类名称，对应一个相应的管理池
            return pool;
        }

        public ObjectPool<T> GetPool<T>() where T : class {
            var type = typeof(T);
            ObjectPool<T> pool = null;
            if (m_ObjectPools.ContainsKey(type.Name)) {
                pool = m_ObjectPools[type.Name] as ObjectPool<T>;
            }
            return pool;
        }

        public T Get<T>() where T : class {
            var pool = GetPool<T>();
            if (pool != null) {
                return pool.Get();
            }
            return default(T);
        }

        public void Release<T>(T obj) where T : class {
            var pool = GetPool<T>();
            if (pool != null) {
                pool.Release(obj);
            }
        }
    }
}