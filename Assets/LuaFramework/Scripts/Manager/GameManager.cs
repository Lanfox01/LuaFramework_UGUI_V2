using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using LuaInterface;
using System.Reflection;
using System.IO;


namespace LuaFramework {
    /// <summary>
    ///  游戏主逻辑管理流程
    /// </summary>
    public class GameManager : Manager {
        protected static bool initialize = false;
        private List<string> downloadFiles = new List<string>();

        /// <summary>
        /// 初始化游戏管理器
        /// </summary>
        void Awake() {
            Init();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        void Init() {
            DontDestroyOnLoad(gameObject);  //防止销毁自己

            CheckExtractResource(); //释放资源
            Screen.sleepTimeout = SleepTimeout.NeverSleep; // 更新的时候不让屏幕休眠
            Application.targetFrameRate = AppConst.GameFrameRate;
        }

        /// <summary>
        /// 判断数据缓存是否存在；第一次不存在要从assetsteam中拷贝过来，
        /// </summary>
        public void CheckExtractResource() {
            // 判断在缓存数据中心是否存在 一般是沙盒路径
            bool isExists = Directory.Exists(Util.DataPath) &&
              Directory.Exists(Util.DataPath + "lua/") && File.Exists(Util.DataPath + "files.txt");
            if (isExists || AppConst.DebugMode) {
                StartCoroutine(OnUpdateResource()); // 路径存在，比如是二次运行的情况下。那么就开启更新 数据的功能
                return;   //文件已经解压过了，自己可添加检查文件列表逻辑
            }
            // 如果缓存数据中心不存在的情况 比如第一次运行
            StartCoroutine(OnExtractResource());    //启动释放协成   从assetsteam中拷贝过来，
        }
        
        // 推测这里的功能 应该是从 包内数据AssetStreaming 第一次 搞到  缓存中心的过程
        IEnumerator OnExtractResource() {
            string dataPath = Util.DataPath;  //数据目录
            string resPath = Util.AppContentPath(); //游戏包资源目录  //一般开始的安装之后默认在 AssetStreaming 中

            if (Directory.Exists(dataPath)) Directory.Delete(dataPath, true);
            Directory.CreateDirectory(dataPath);

            string infile = resPath + "files.txt";   // 源数据地址
            string outfile = dataPath + "files.txt"; // 目标地址
            if (File.Exists(outfile)) File.Delete(outfile);

            string message = "正在解包文件:>files.txt";
            Debug.Log(infile);
            Debug.Log(outfile);
            if (Application.platform == RuntimePlatform.Android) {
                WWW www = new WWW(infile);
                yield return www;

                if (www.isDone) {
                    File.WriteAllBytes(outfile, www.bytes);
                }
                yield return 0;
            } else File.Copy(infile, outfile, true);
            yield return new WaitForEndOfFrame();

            //释放所有文件到数据目录
            string[] files = File.ReadAllLines(outfile);
            foreach (var file in files) {
                string[] fs = file.Split('|');
                infile = resPath + fs[0];  //
                outfile = dataPath + fs[0];

                message = "正在解包文件:>" + fs[0];
                Debug.Log("正在解包文件:>" + infile);
                facade.SendMessageCommand(NotiConst.UPDATE_MESSAGE, message); // 解压或者拷贝一个文件就发送一个命令消息  可以作为加载进度条 但是这个命令好像一开始没有注册 
                                                                            // 为什么不写成 AppFacade.Instance.SendMessageCommand(NotiConst.UPDATE_MESSAGE, message); 因为他们的本质集成源头是一样的 所以属于框架内 走变量，不走属性；
                string dir = Path.GetDirectoryName(outfile);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                if (Application.platform == RuntimePlatform.Android) {
                    WWW www = new WWW(infile);
                    yield return www;

                    if (www.isDone) {
                        File.WriteAllBytes(outfile, www.bytes);
                    }
                    yield return 0;
                } else {
                    if (File.Exists(outfile)) {
                        File.Delete(outfile);
                    }
                    File.Copy(infile, outfile, true);
                }
                yield return new WaitForEndOfFrame();
            }
            message = "解包完成!!!"; // 上面这么多数据如果中途出现数据错误怎么办？
            facade.SendMessageCommand(NotiConst.UPDATE_MESSAGE, message);
            yield return new WaitForSeconds(0.1f); //全部解压之后为什么要等 0.1 秒？

            message = string.Empty;
            //释放完成，开始启动更新资源 ； 因为有可能下载的安装包不是最新的，随意必须得再走一遍更新流程
            StartCoroutine(OnUpdateResource());
        }

        /// <summary>
        /// 从资源服务器上下载资源
        /// 启动更新下载，这里只是个思路演示，此处可启动线程下载更新；更新的数据就存入到数据缓存中心；
        /// </summary>
        IEnumerator OnUpdateResource() {
            if (!AppConst.UpdateMode) {
                OnResourceInited(); // 这里是不更新数据，直接跳刀结束的时候，比如离线设置，或者系统设置不默认更新的情况；
                yield break; 
            }
            // 具体的 需要 刷新数据的模块； 似乎用到 www 可能有点过时？ 注意给他配置一个资源服务器，端口地址查看修改  AppConst.WebUrl
            //  这里客户端的下载路径时间 怎么跟服务端对上？
            string dataPath = Util.DataPath;  //数据目录
            string url = AppConst.WebUrl;
            string message = string.Empty;
            string random = DateTime.Now.ToString("yyyymmddhhmmss"); // 这里到底是怎么设计的？怎么可以匹配到服务器的更新包？感觉服务器应该有一个后端处理PHP？
            string listUrl = url + "files.txt?v=" + random;
            Debug.LogWarning("LoadUpdate---->>>" + listUrl);//最终这给到的资源下载地址是什么？为什么要这么长 时间错什么的，能对的上资源吗

            WWW www = new WWW(listUrl); yield return www;
            if (www.error != null) {
                OnUpdateFailed(string.Empty);
                yield break;
            }
            if (!Directory.Exists(dataPath)) {
                Directory.CreateDirectory(dataPath);
            }
            File.WriteAllBytes(dataPath + "files.txt", www.bytes); // 这个 files.txt是总纲文件吗？
            string filesText = www.text;
            string[] files = filesText.Split('\n'); // 下面都是根据总纲文件中给的地址 以此索引下载，基本上都是文本文件

            for (int i = 0; i < files.Length; i++) {
                if (string.IsNullOrEmpty(files[i])) continue;
                string[] keyValue = files[i].Split('|');
                string f = keyValue[0];
                string localfile = (dataPath + f).Trim();
                string path = Path.GetDirectoryName(localfile);
                if (!Directory.Exists(path)) {
                    Directory.CreateDirectory(path);
                }
                string fileUrl = url + f + "?v=" + random;
                bool canUpdate = !File.Exists(localfile);
                if (!canUpdate) { //文件 md5 校验，无法通过表示可能被恶意修改了，所以这个文件不能被更新，并且删掉这个
                    string remoteMd5 = keyValue[1].Trim();
                    string localMd5 = Util.md5file(localfile);
                    canUpdate = !remoteMd5.Equals(localMd5);
                    if (canUpdate) File.Delete(localfile);
                }
                if (canUpdate) {   //本地缺少文件  那就是更新包的文件是新加的，那么很好，直接下载下来
                    Debug.Log(fileUrl);
                    message = "downloading>>" + fileUrl;
                    facade.SendMessageCommand(NotiConst.UPDATE_MESSAGE, message);
                    /*
                    www = new WWW(fileUrl); yield return www;
                    if (www.error != null) {
                        OnUpdateFailed(path);   //
                        yield break;
                    }
                    File.WriteAllBytes(localfile, www.bytes);
                     */
                    //这里都是资源文件，用线程下载
                    BeginDownload(fileUrl, localfile);
                    while (!(IsDownOK(localfile))) { yield return new WaitForEndOfFrame(); }
                }
            }
            yield return new WaitForEndOfFrame();

            message = "更新完成!!";
            facade.SendMessageCommand(NotiConst.UPDATE_MESSAGE, message);

            OnResourceInited();
        }

        void OnUpdateFailed(string file) {
            string message = "更新失败!>" + file;
            facade.SendMessageCommand(NotiConst.UPDATE_MESSAGE, message);
        }

        /// <summary>
        /// 是否下载完成
        /// </summary>
        bool IsDownOK(string file) {
            return downloadFiles.Contains(file);
        }

        /// <summary>
        /// 线程下载
        /// </summary>
        void BeginDownload(string url, string file) {     //线程下载
            object[] param = new object[2] { url, file };

            ThreadEvent ev = new ThreadEvent();
            ev.Key = NotiConst.UPDATE_DOWNLOAD;
            ev.evParams.AddRange(param);
            ThreadManager.AddEvent(ev, OnThreadCompleted);   //线程下载
        }

        /// <summary>
        /// 线程完成
        /// </summary>
        /// <param name="data"></param>
        void OnThreadCompleted(NotiData data) {
            switch (data.evName) {
                case NotiConst.UPDATE_EXTRACT:  //解压一个完成
                //
                break;
                case NotiConst.UPDATE_DOWNLOAD: //下载一个完成
                downloadFiles.Add(data.evParam.ToString());
                break;
            }
        }

        /// <summary>
        /// 资源初始化结束
        /// </summary>
        public void OnResourceInited() {
#if ASYNC_MODE //如果是异步模式？
            ResManager.Initialize(AppConst.AssetDir, delegate() {
                Debug.Log("Initialize OK!!!");
                this.OnInitialize();
            });
#else
            ResManager.Initialize();
            this.OnInitialize();
#endif
        }
 /// <summary>
 ///  资源都准备完毕之后，开始 业务逻辑也就是Lua 热更这块了
 ///  // 如果前面对框架使用都没任何问题， 真正的游戏主逻辑结构是写到这里的
 /// </summary>
        void OnInitialize() {
            LuaManager.InitStart(); //lua 虚拟机完美启动 接下来是可以 写 Lua 逻辑代码了
            LuaManager.DoFile("Logic/Game");         //加载Lua端游戏 模块
            //网络初始化
            LuaManager.DoFile("Logic/Network");      //加载lua端网络模块  网络的使用初始化交到Lua端控制？
            NetManager.OnInit();                     //初始化网络   这里C#端控制了在写在lua端的网络监听；

            Util.CallMethod("Game", "OnInitOK");     //初始化完成  调用的应该是 "Logic/Game" 里面的OnInitOk 方法 这里有写死了一些 服务端的IP 和端口信息；

            initialize = true;
            /*
              以下这段是测试 缓冲池模块的功能，如果游戏没有涉及大量的重复生产，可以不考虑，比如子弹之类的 
            */ 

            // //类对象池测试 就是一般类的创建？？  一般情况 生成一次 单例 不建议多次调用？ 动词调用会不会被覆盖？
            // var classObjPool = ObjPoolManager.CreatePool<TestObjectClass>(OnPoolGetElement, OnPoolPushElement);// 什么意思？？创建之后，注册到 对象池
           
            // //方法1
            // //objPool.Release(new TestObjectClass("abcd", 100, 200f));
            // //var testObj1 = objPool.Get();

            // //方法2
            // ObjPoolManager.Release<TestObjectClass>(new TestObjectClass("abcd", 100, 200f));// 这个释放 为什么还要 new ?  会带动释放的绑定事件
            // // 理解下这里的释放，就是把这个对象预先压入缓存池，并且置顶第一位 下次再去Get 获取的时候 就会取到它
            // // 一般来说  ObjPoolManager.CreatePool 和 ObjPoolManager.Release 都是成双 出现的
            //  UnityCShapDebugMrg.Dbug("某种类型的对象池  Release classObjPool  countAll:"+ classObjPool.countInactive); 
           
            // var testObj1 = ObjPoolManager.Get<TestObjectClass>(); //这里从对象池里获取某一个对象， 会触发 OnPoolGetElement 这其实就是取到上面那个类实例
         
            // Debugger.Log("TestObjectClass--->>>" + testObj1.ToString());
            // UnityCShapDebugMrg.Dbug("某种类型的对象池  Release classObjPool  countAll:"+ classObjPool.countInactive); //上面有一个 Get 了，为什么这里还是0？因为这些属性仅正对游戏对象实例有用

            // //游戏对象池测试  游戏对象类的创建？ 显然创建实例对象是没有存在 绑定事件这种说法的
            // var prefab = Resources.Load("TestGameObjectPrefab", typeof(GameObject)) as GameObject; // 这个加载东西就是一个空对象啊，示意可以其他的？
            // var gameObjPool = ObjPoolManager.CreatePool("TestGameObject", 5, 10, prefab); // 这里是根据 池子名字设置的 查询的；并且规定池子是存放GameObject类型 

            // var gameObj = Instantiate(prefab) as GameObject; // 这里为什么不从池子里取，直接这样子实例化的话初始一遍？然后在通过 Release 存入缓冲池
            // gameObj.name = "TestGameObject_01";
            // gameObj.transform.localScale = Vector3.one;
            // gameObj.transform.localPosition = Vector3.zero;

            // ObjPoolManager.Release("TestGameObject", gameObj); //示范掉当前，放回一个位置， 正常的话跟上面  ObjPoolManager.CreatePool 成对出现，那么上面的话还有4个位置？
            // // 这个 release 不是说把整个池子 撤销掉； 那如果想撤销掉怎么办？
            // var backObj = ObjPoolManager.Get("TestGameObject"); //  然后用的时候 继续拿出来用，此时又变成从缓冲池拿出来，这个会不会太复杂
            // backObj.transform.SetParent(null);
            // UnityCShapDebugMrg.Dbug("获取缓存池中的下一个对象："+ gameObjPool.NextAvailableObject().name);
            // Debug.Log("TestGameObject--->>>" + backObj); // TestGameObject_01 这里的代码不是最后一句日志； 还有 lua 中创建的 Pannel Awake下一帧会有提示；
        }

        /// <summary>
        /// 当从池子里面获取时
        /// </summary>
        /// <param name="obj"></param>
        void OnPoolGetElement(TestObjectClass obj) {
            //Debug.Log("OnPoolGetElement--->>>" + obj);
            UnityCShapDebugMrg.Dbug("当从池子里面获取时",UnityCShapDebug.InfoLevel.worm);
        }

        /// <summary>
        /// 当放回池子里面时
        /// </summary>
        /// <param name="obj"></param>
        void OnPoolPushElement(TestObjectClass obj) {
             //Debug.Log("OnPoolPushElement--->>>" + obj);
             UnityCShapDebugMrg.Dbug("当放回池子里面时",UnityCShapDebug.InfoLevel.worm);
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        void OnDestroy() {
            if (NetManager != null) {
                NetManager.Unload();
            }
            if (LuaManager != null) {
                LuaManager.Close();
            }
            Debug.Log("~GameManager was destroyed");
        }
    }
}