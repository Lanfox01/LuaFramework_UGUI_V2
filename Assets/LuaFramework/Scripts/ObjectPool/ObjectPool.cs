using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LuaFramework
{

    /// <summary>
    ///  一般对象池？？ 就是预设了 N 个，然后用一个get，然后就会pop堆栈中减少一个； 释放就会存入到堆栈中多一个；并且带动事件变化
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObjectPool<T> where T : class 
    {
        private readonly Stack<T> m_Stack = new Stack<T>(); // 这个默认是多大？ 0？
        private readonly UnityAction<T> m_ActionOnGet;
        private readonly UnityAction<T> m_ActionOnRelease;

        public int countAll { get; private set; }  
        public int countActive { get { return countAll - countInactive; } }  
        public int countInactive { get { return m_Stack.Count; } }  

        public ObjectPool(UnityAction<T> actionOnGet, UnityAction<T> actionOnRelease)
        {
            m_ActionOnGet = actionOnGet;
            m_ActionOnRelease = actionOnRelease;
        }

        public T Get()
        {
            T element = m_Stack.Pop(); // 删除最顶部的一个元素，并且返回这个元素
            if (m_ActionOnGet != null)
                m_ActionOnGet(element);
            return element;
        }

        public void Release(T element)  
        {
            if (m_Stack.Count > 0 && ReferenceEquals(m_Stack.Peek(), element)) // 获取栈顶值，只是获取不删除 跟 pop 不一样
                Debug.LogError("Internal error. Trying to destroy object that is already released to pool.");
            if (m_ActionOnRelease != null)
                m_ActionOnRelease(element);
            m_Stack.Push(element); // 压入到最顶部一个元素；
        }
    }
}
