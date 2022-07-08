/* 
 LuaFramework Code By Jarjin leeibution 3.0 License 
*/
using System;
using System.Collections.Generic;

/// <summary>
///  接口，定义各种想要的功能； 具体的实现由 派生类Controller 负责；
/// </summary>
public interface IController
{
    void RegisterCommand(string messageName, Type commandType);
    void RegisterViewCommand(IView view, string[] commandNames);

    void ExecuteCommand(IMessage message);

	void RemoveCommand(string messageName);
    void RemoveViewCommand(IView view, string[] commandNames);

	bool HasCommand(string messageName);
}
