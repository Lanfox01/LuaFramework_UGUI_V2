using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class AppFacade : Facade // 框架管理类
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
    }

    /// <summary>
    /// 启动框架
    /// </summary>
    public void StartUp() {
        SendMessageCommand(NotiConst.START_UP); // 发送命令启动简讯，  他会从命令中心去获取到之前 注册的 类型
        RemoveMultiCommand(NotiConst.START_UP); // 至于为什么要移除？ 是什么意思？
    }
}

