/* 
 LuaFramework Code By Jarjin lee
*/

using System;
using System.Collections.Generic;
 // 控制中心，或者是 指令中心   实现的核心主要是对下面两个库的管理， 加和减；
public class Controller : IController {
    protected IDictionary<string, Type> m_commandMap; // 一般命令库
    protected IDictionary<IView, List<string>> m_viewCmdMap;  // 视图命令库

    protected static volatile IController m_instance; // 单例？
    protected readonly object m_syncRoot = new object();
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
            if (m_commandMap.ContainsKey(note.Name)) {
                commandType = m_commandMap[note.Name];
            } else {
                views = new List<IView>();
                foreach (var de in m_viewCmdMap) {
                    if (de.Value.Contains(note.Name)) {
                        views.Add(de.Key);
                    }
                }
            }
        }
        if (commandType != null) {  //Controller
            object commandInstance = Activator.CreateInstance(commandType);
            if (commandInstance is ICommand) {
                ((ICommand)commandInstance).Execute(note); // 这里应该是重点； 当一个普通命令进来的时候，就是表示 需要实例化 一个类实例对象；比如 NotiConst.START_UP 命令就是对应实例化 StartUpCommand
            }
        }
        if (views != null && views.Count > 0) {
            for (int i = 0; i < views.Count; i++) {
                views[i].OnMessage(note);
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

