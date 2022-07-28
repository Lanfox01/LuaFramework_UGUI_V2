using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace LuaFramework {

	[Serializable]
	public class PoolInfo {
		public string poolName;
		public GameObject prefab;
		public int poolSize;
		public bool fixedSize;
	}

	public class GameObjectPool {
        private int maxSize; // 最大数量
		private int poolSize; // 池子大小，一般在初始构造中就决定了，改变这个值可以一下子初始化多少个出来
		private string poolName; // 池子名字
        private Transform poolRoot; // 池子 根节点
        private GameObject poolObjectPrefab; // 池子中对象的实例原型
        private Stack<GameObject> availableObjStack = new Stack<GameObject>(); // 池子可能就是用这个 堆栈管理的 
		// 预设某个类 在内存中存在的池子，并且设置这个池子的大小，以及预设了多少个实例对象，并且隐藏他们；
        public GameObjectPool(string poolName, GameObject poolObjectPrefab, int initCount, int maxSize, Transform pool) {
			this.poolName = poolName;
			this.poolSize = initCount;
            this.maxSize = maxSize;
            this.poolRoot = pool;
            this.poolObjectPrefab = poolObjectPrefab;

			//populate the pool
			for(int index = 0; index < initCount; index++) {
				AddObjectToPool(NewObjectInstance());
			}
		}

		//o(1)
        private void AddObjectToPool(GameObject go) {
			//add to pool
            go.SetActive(false);
            availableObjStack.Push(go);
            go.transform.SetParent(poolRoot, false);
		}

        private GameObject NewObjectInstance() {
            return GameObject.Instantiate(poolObjectPrefab) as GameObject;
		}

		public GameObject NextAvailableObject() {
            GameObject go = null;
			if(availableObjStack.Count > 0) {
				go = availableObjStack.Pop();
			} else {
				Debug.LogWarning("No object available & cannot grow pool: " + poolName);
			}
            go.SetActive(true);
            return go;
		} 
		
		//o(1)
        public void ReturnObjectToPool(string pool, GameObject po) {
            if (poolName.Equals(pool)) {
                AddObjectToPool(po);
			} else {
				Debug.LogError(string.Format("Trying to add object to incorrect pool {0} ", poolName));
			}
		}
	}
}
