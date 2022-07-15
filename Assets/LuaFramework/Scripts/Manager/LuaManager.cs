using UnityEngine;
using System.Collections;
using LuaInterface;

namespace LuaFramework {
    public class LuaManager : Manager {
        private LuaState lua;
        private LuaLoader loader;
        private LuaLooper loop = null;

        // Use this for initialization
        void Awake() {
            loader = new LuaLoader();
            lua = new LuaState();
            this.OpenLibs();
            lua.LuaSetTop(0);

            LuaBinder.Bind(lua);
            DelegateFactory.Init();
            LuaCoroutine.Register(lua, this);
        }
//如果到下面这一列代码如果有上面报错，那说明lua虚拟没有启动成功，或者一些wrap  binder 等文件没弄清楚；
        public void InitStart() {
            InitLuaPath();
            InitLuaBundle();
            this.lua.Start();    //启动LUAVM
            this.StartMain();
            this.StartLooper();
        }

        void StartLooper() {
            loop = gameObject.AddComponent<LuaLooper>();// 不是很懂这个做什么， 推荐可能是为了给lua端划分跟unity 一样的三大帧事件
            loop.luaState = lua;
        }

        //cjson 比较特殊，只new了一个table，没有注册库，这里注册一下
        protected void OpenCJson() {
            lua.LuaGetField(LuaIndexes.LUA_REGISTRYINDEX, "_LOADED");
            lua.OpenLibs(LuaDLL.luaopen_cjson);
            lua.LuaSetField(-2, "cjson");

            lua.OpenLibs(LuaDLL.luaopen_cjson_safe);
            lua.LuaSetField(-2, "cjson.safe");
        }

        void StartMain() {               
            // 在c# 端 调用一个 lua function的具体过程 步骤；  （tolua. 其他的lua 估计也是大同小异）
            lua.DoFile("Main.lua");  // 一般写法就是这样子， dofile 引入lua文件，当然b必须在它的 AddSearchPath 下，这句话才不会报错；
            LuaFunction main = lua.GetFunction("Main"); // 接着才能在虚拟机中找到 这个Fun 具体的方法 mian（）
            main.Call(); // 然后调用它 call()；
            main.Dispose();// 不要就释放掉；
            main = null;    
        }
        
        /// <summary>
        /// 初始化加载第三方库
        /// </summary>
        void OpenLibs() {
            lua.OpenLibs(LuaDLL.luaopen_pb);      
            lua.OpenLibs(LuaDLL.luaopen_sproto_core);
            lua.OpenLibs(LuaDLL.luaopen_protobuf_c);
            lua.OpenLibs(LuaDLL.luaopen_lpeg);
            lua.OpenLibs(LuaDLL.luaopen_bit);
            lua.OpenLibs(LuaDLL.luaopen_socket_core);

            this.OpenCJson();
        }

        /// <summary>
        /// 初始化Lua代码加载路径
        /// 编辑模式就在 工程目录/LuaFramework/ 下面的 两个子目录中查找： "/Lua"  "/ToLua/Lua"
        /// </summary>
        void InitLuaPath() {
            if (AppConst.DebugMode) {
                string rootPath = AppConst.FrameworkRoot;
                lua.AddSearchPath(rootPath + "/Lua");
                lua.AddSearchPath(rootPath + "/ToLua/Lua");
            } else {
                lua.AddSearchPath(Util.DataPath + "lua");
            }
        }

        /// <summary>
        /// 初始化LuaBundle
        /// </summary>
        void InitLuaBundle() { //可以参考对比下 公司的那个艾代码； 问题：如果不是基础的Lua,是否要加入这里？ 数了下外面。unity3d的包有20个 这里只有16个
            if (loader.beZip) {// 这里为真； 下面一般是针对于lua 出的bundle包名字，一般这些lua 必然是非常基础功能的；不是逻辑业务的lua,正常一般不改它们的名字
                loader.AddBundle("lua/lua.unity3d"); // 有没有发现？后缀名都是 unity3d 这个在出包的时候 也必须考虑进去；
                loader.AddBundle("lua/lua_math.unity3d"); 
                loader.AddBundle("lua/lua_system.unity3d"); // 这个实际的目录是 lua/lua_system.unity3d 这是一个bundle的路径，通过这个代码可以解析包里面的代码；
                loader.AddBundle("lua/lua_system_reflection.unity3d");
                loader.AddBundle("lua/lua_unityengine.unity3d");
                loader.AddBundle("lua/lua_common.unity3d");
                loader.AddBundle("lua/lua_logic.unity3d");
                loader.AddBundle("lua/lua_view.unity3d");
                loader.AddBundle("lua/lua_controller.unity3d");
                loader.AddBundle("lua/lua_misc.unity3d");

                loader.AddBundle("lua/lua_protobuf.unity3d");
                loader.AddBundle("lua/lua_3rd_cjson.unity3d");
                loader.AddBundle("lua/lua_3rd_luabitop.unity3d");
                loader.AddBundle("lua/lua_3rd_pbc.unity3d");
                loader.AddBundle("lua/lua_3rd_pblua.unity3d");
                loader.AddBundle("lua/lua_3rd_sproto.unity3d");
                //对比发现没有加进来的有 ： lua_cjson.unity3d   lua_jit.unity3d lua_lpeg.unity3d  lua_socket.unity3d  lua_system_injection
            }
        }

        public void DoFile(string filename) {
            lua.DoFile(filename);
        }

        // Update is called once per frame
        public object[] CallFunction(string funcName, params object[] args) {
            LuaFunction func = lua.GetFunction(funcName);
            if (func != null) {
                return func.LazyCall(args);
            }
            return null;
        }

        public void LuaGC() {
            lua.LuaGC(LuaGCOptions.LUA_GCCOLLECT);
        }

        public void Close() {
            loop.Destroy();
            loop = null;

            lua.Dispose();
            lua = null;
            loader = null;
        }
    }
}