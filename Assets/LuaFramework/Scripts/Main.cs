using UnityEngine;
using System.Collections;

namespace LuaFramework {

    /// <summary>
    /// </summary>
    public class Main : MonoBehaviour {

        void Start() {
            AppFacade.Instance.StartUp();   //启动游戏
        }
    }
}

/*
 * 问题表：
 * 1 NotiConst 命令中，为什么要分为两类来处理； 一般命令和 view命令； 到底在本质上有上面不同；
 *  为什么在使用一般命令的时候需要提前注册 RegisterCommand ；而 view命令视乎不需要；
 *  另外 响应一般命令一般都是实例化一个类实例， 但是监听 view 命令具体是做啥的？
 *
 * LuaLooper 这个代码做啥用的，为什么一开始 start 就 异常为空报错？
 */