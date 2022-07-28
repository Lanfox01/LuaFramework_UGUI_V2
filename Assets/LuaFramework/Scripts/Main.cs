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

/*新手步骤： 在 特定的目录下 LuaFramework/Lua/ 添加修改代码，如果新建的 .lua代码不能命名不能有 _ 下划线；
 *  菜单工具lua/  Generator 生成代Lua 中间代码；  
 *  菜单工具luaFramework 生成对应平台的 比如 android/Resources
 *
 *文件的生成过程， LuaFramework/Lua/ 自定义lua文件，---> 工程/lua 临时文件，分别处理.lua和非.lua文件格式， 前者后面加 xxx.lua.byte; --->streamAsset 目录文件；  然后把这些文件 弄到某个资源服务器上
 * 运行的时候： 1  第一次安装运行 直接从 StreamAsset中拷贝过来， 2 之后 会检查更新， 再次验证，从 服务器地址上进行验证比对；   1和2 都会下载到 运行平台的 数据缓存中心；
 *  主要涉及到文件相对路径转移，和路径的寻址， 以及toLua 加载路径的定义； 还有就是 文档文件的MD5 验证；
 * 
 * 问题表：
 * 1 NotiConst 命令中，为什么要分为两类来处理； 一般命令和 view命令； 到底在本质上有上面不同；
 *  为什么在使用一般命令的时候需要提前注册 RegisterCommand ；而 view命令视乎不需要；
 *  另外 响应一般命令一般都是实例化一个类实例， 但是监听 view 命令具体是做啥的？  问题的答案 主要在 controller
 *
 * LuaLooper 这个代码做啥用的，为什么一开始 start 就 异常为空报错？
 *
 * 对比下NetworkManager 和 Network.lua  为什么要这么 分散设计
 *
 *ToLua/Source/Generate 下面的文件是否被打包bundle 并且存入到 assetstreaming 里面？
 *
 * LuaFramework/Lua/"      自定义具体项目lua脚本的目录
 * 和  LuaFramework/ToLua/lua  Tolua封装好的工具集或插件 
 */