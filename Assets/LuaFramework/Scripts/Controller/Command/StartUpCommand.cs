using UnityEngine;
using System.Collections;
using LuaFramework;

public class StartUpCommand : ControllerCommand {

    public override void Execute(IMessage message) {
        if (!Util.CheckEnvironment()) return;

        GameObject gameMgr = GameObject.Find("GlobalGenerator"); // 全局生成器  好像是针对 全局 View的根目录？ 好像 没啥软用？ 可以试着加一个这种对象；
        if (gameMgr != null) {
            AppView appView = gameMgr.AddComponent<AppView>();
        }
        //-----------------关联命令-----------------------
        AppFacade.Instance.RegisterCommand(NotiConst.DISPATCH_MESSAGE, typeof(SocketCommand));// 这里注册下；但凡有人发送 这个DISPATCH_MESSAGE命令，就表示开启网络功能； 这里谁来调用？
        // NetworkManager 在这里有调用到；  SocketCommand也就是可能属于NetworkManager的补充模块；
        
        //-----------------初始化管理器----------------------- 基本上是就是框架的所有功能预览了
        AppFacade.Instance.AddManager<LuaManager>(ManagerName.Lua); // ManagerName 这里只是名字方便管理的一种地方 方式；
        AppFacade.Instance.AddManager<PanelManager>(ManagerName.Panel);
        AppFacade.Instance.AddManager<SoundManager>(ManagerName.Sound);
        AppFacade.Instance.AddManager<TimerManager>(ManagerName.Timer);
        AppFacade.Instance.AddManager<NetworkManager>(ManagerName.Network);
        AppFacade.Instance.AddManager<ResourceManager>(ManagerName.Resource); // 负责 资源的管理 加载 释放 主要是 控制assetbundle 来管理内存； 这可能是优化重点；里面很多方法和逻辑没看懂
        AppFacade.Instance.AddManager<ThreadManager>(ManagerName.Thread);
        AppFacade.Instance.AddManager<ObjectPoolManager>(ManagerName.ObjectPool);
        AppFacade.Instance.AddManager<GameManager>(ManagerName.Game); // 推荐这个模块放到最后； 毕竟这个是游戏逻辑管理的入口 模块；
        // 如果我想要开辟新的框架 功能模块 比如
        //  AppFacade.Instance.AddManager<SDKManager>(ManagerName.SDK); // SDK 管理接入模块
        //  AppFacade.Instance.AddManager<GoogleTranslationManager>(ManagerName.GoogleTranslation); //全局翻译模块
    }
}