/* 
 LuaFramework Code By Jarjin lee
*/

using System;
using System.Collections.Generic;
 // 控制中心，或者是 指令中心   实现的核心主要是对下面两个库的管理， 加和减；
public class Controller : IController {
    protected IDictionary<string, Type> m_commandMap; // 一般命令库
    protected IDictionary<IView, List<string>> m_viewCmdMap;  // 视图命令库  按照元素理解，是不是一个视图对应有多个命令?  具体这里有多少视图？
    // 每个视图 对应多少相关的命令？

    protected static volatile IController m_instance; // 单例？
    protected readonly object m_syncRoot = new object(); // 这个锁不锁 是不是跟视图命令和一般命令也有影响？
    protected static readonly object m_staticSyncRoot = new object();

    protected Controller() {
        InitializeController();
    }

    static Controller() {
    }

    public static IController Instance {
        get {
            if (m_instance == null) {
                lock (m_staticSyncRoot) {
                    if (m_instance == null) m_instance = new Controller();
                }
            }
            return m_instance;
        }
    }

    protected virtual void InitializeController() {
        m_commandMap = new Dictionary<string, Type>();  // 控制中心 有两大 命令存储库；   一般命令
        m_viewCmdMap = new Dictionary<IView, List<string>>();  //  视图命令
    }

    public virtual void ExecuteCommand(IMessage note) {
        Type commandType = null;
        List<IView> views = null;
        lock (m_syncRoot) {
            if (m_commandMap.ContainsKey(note.Name)) { //命令集中是否包含 这个 键值
                commandType = m_commandMap[note.Name]; // 有的话，就找出这键值对应的值 一般来说值 就是一个什么样的类型
            } else {
                views = new List<IView>();
                foreach (var de in m_viewCmdMap) {
                    if (de.Value.Contains(note.Name)) {
                        views.Add(de.Key); // 遍历 m_viewCmdMap 中，但凡有发现各个视图中有涉及或者监听到这条命令的，把这些视图都添加归类到 views;
                    }
                }
            }
        }
        // 什么情况不为空？1 一般命令 2 就是上面的运行发现，该指令是之前注册过的，这里才会对应的实例化一个相应的类实例；
        if (commandType != null) {  //Controller 
            object commandInstance = Activator.CreateInstance(commandType);
            if (commandInstance is ICommand) {
                ((ICommand)commandInstance).Execute(note); // 这里应该是重点； 当一个普通命令进来的时候，就是表示 需要实例化 一个类实例对象；比如 NotiConst.START_UP 命令就是对应实例化 StartUpCommand
            }
        }
        // 响应的当处理 视图命令的时候 
        if (views != null && views.Count > 0) {
            for (int i = 0; i < views.Count; i++) {
                views[i].OnMessage(note); // 这里遍历 上面抓捕到的相关视图，并且给这些视图依次发送 信号；；
            }
            views = null;
        }
    }

    public virtual void RegisterCommand(string commandName, Type commandType) {
        lock (m_syncRoot) {
            m_commandMap[commandName] = commandType;
        }
    }

    public virtual void RegisterViewCommand(IView view, string[] commandNames) {
        lock (m_syncRoot) {
            if (m_viewCmdMap.ContainsKey(view)) {
                List<string> list = null;
                if (m_viewCmdMap.TryGetValue(view, out list)) {
                    for (int i = 0; i < commandNames.Length; i++) {
                        if (list.Contains(commandNames[i])) continue;
                        list.Add(commandNames[i]);
                    }
                }
            } else {
                m_viewCmdMap.Add(view, new List<string>(commandNames));
            }
        }
    }

    public virtual bool HasCommand(string commandName) {
        lock (m_syncRoot) {
            return m_commandMap.ContainsKey(commandName);
        }
    }

    public virtual void RemoveCommand(string commandName) {
        lock (m_syncRoot) {
            if (m_commandMap.ContainsKey(commandName)) {
                m_commandMap.Remove(commandName);
            }
        }
    }

    public virtual void RemoveViewCommand(IView view, string[] commandNames) {
        lock (m_syncRoot) {
            if (m_viewCmdMap.ContainsKey(view)) {
                List<string> list = null;
                if (m_viewCmdMap.TryGetValue(view, out list)) {
                    for (int i = 0; i < commandNames.Length; i++) {
                        if (!list.Contains(commandNames[i])) continue;
                        list.Remove(commandNames[i]);
                    }
                }
            }
        }
    }
}

