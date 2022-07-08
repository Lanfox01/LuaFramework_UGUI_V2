using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
///    框架管理类 负责整个框架的整体功能，启动，停止啥的；对外的引用入口； 具体的各种功能 封装到基类中 base()
///    一般调用的话，如果不改变框架内部结构， 只要使用 AppFacede 提供的各种接口就可以
/// </summary>
public class AppFacade : Facade
{
    private static AppFacade _instance; // 单例

    public AppFacade() : base()
    {
    }

    public static AppFacade Instance
    {
        get{
            if (_instance == null) {
                _instance = new AppFacade();
            }
            return _instance;
        }
    }

    override protected void InitFramework()
    {
        base.InitFramework(); // 基类中初始化框架
        RegisterCommand(NotiConst.START_UP, typeof(StartUpCommand)); // 把命令 注册或者存入 命令中心  其实是一个字典；之后输入 启动命令就可以 执行实例化对应的 类实例；
        // 注意 StartUpCommand 这个命令的派生， 当有新的名需要增加的时候，直接修改这个类，或者 参考它的写法； 派生于ControllerCommand ；
        // 说白了，整个框架的核心就是 命令 和 命令处理中心（控制中心）  整套的 命令管理系统；
    }

    /// <summary>
    /// 启动框架
    /// </summary>
    public void StartUp() {
        SendMessageCommand(NotiConst.START_UP); // 发送命令启动简讯，  他会从命令中心去获取到之前 注册的 类型
        RemoveMultiCommand(NotiConst.START_UP); // 至于为什么要移除？ 是什么意思？
    }
}

