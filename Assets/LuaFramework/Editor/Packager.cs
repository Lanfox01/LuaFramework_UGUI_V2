using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using LuaFramework;
using Debug = UnityEngine.Debug;

/// <summary>
///  这个是一个核心的打包 工具 封装；
/// </summary>

public class Packager {
    public static string platform = string.Empty;
    static List<string> paths = new List<string>();
    static List<string> files = new List<string>();
    static List<AssetBundleBuild> maps = new List<AssetBundleBuild>();

    ///-----------------------------------------------------------
    static string[] exts = { ".txt", ".xml", ".lua", ".assetbundle", ".json" };
    static bool CanCopy(string ext) {   //能不能复制
        foreach (string e in exts) {
            if (ext.Equals(e)) return true;
        }
        return false;
    }

    /// <summary>
    /// 载入素材
    /// </summary>
    static UnityEngine.Object LoadAsset(string file) {
        if (file.EndsWith(".lua")) file += ".txt";
        return AssetDatabase.LoadMainAssetAtPath("Assets/LuaFramework/Examples/Builds/" + file);
    }

    [MenuItem("LuaFramework/Build iPhone Resource", false, 100)]
    public static void BuildiPhoneResource() {
        BuildTarget target = BuildTarget.iOS;
        BuildAssetResource(target);
    }

    [MenuItem("LuaFramework/Build Android Resource", false, 101)]
    public static void BuildAndroidResource() {
        BuildAssetResource(BuildTarget.Android);
    }

    [MenuItem("LuaFramework/Build Windows Resource", false, 102)]
    public static void BuildWindowsResource() {
        BuildAssetResource(BuildTarget.StandaloneWindows);
    }

    /// <summary>
    /// 生成绑定素材
    /// </summary>
    public static void BuildAssetResource(BuildTarget target) {
        if (Directory.Exists(Util.DataPath)) {
            Directory.Delete(Util.DataPath, true);  // 删除数据 缓存中心 中转站 比如 window 在 C盘根目录下
        }
        string streamPath = Application.streamingAssetsPath;  // 删除数据 初始化中心
        if (Directory.Exists(streamPath)) {
            Directory.Delete(streamPath, true);
        }
        Directory.CreateDirectory(streamPath);
        AssetDatabase.Refresh();

        maps.Clear(); //要打 bundel 包的管理仓库 清空
        if (AppConst.LuaBundleMode) { // 是否要打包 模式
            HandleLuaBundle();
        } else {
            HandleLuaFile();
        }
        if (AppConst.ExampleMode) {
            HandleExampleBundle();
        }
        string resPath = "Assets/" + AppConst.AssetDir;
        BuildPipeline.BuildAssetBundles(resPath, maps.ToArray(), BuildAssetBundleOptions.None, target); // 出包
        BuildFileIndex();

        // string streamDir = Application.dataPath + "/" + AppConst.LuaTempDir;
        // if (Directory.Exists(streamDir)) Directory.Delete(streamDir, true);
        // AssetDatabase.Refresh();
    }
    // 写的什么逻辑，最终的目的就是为了 压入 打成bundle  包 ；
    static void AddBuildMap(string bundleName, string pattern, string path) {
        string[] files = Directory.GetFiles(path, pattern);
        if (files.Length == 0) return;
        // 如果是只有子目录的情况 下面的子文件
        for (int i = 0; i < files.Length; i++) {
            files[i] = files[i].Replace('\\', '/');
            Debug.Log(files[i]);
        }
        AssetBundleBuild build = new AssetBundleBuild();
        build.assetBundleName = bundleName; // 为打出的assetbundle名
        build.assetNames = files;  //  为 要打包的资源路径 这里使用相对路径，它是一个数组，说明可以把多个对象打成一个assetbundle。 一般是 Assets开头 比如 ：Assets/Lua/3rd
        maps.Add(build);
    }

    /// <summary>
    /// 处理Lua代码包    --- 这里涉及的是针对两个特定目录下 .lua 格式的包，那么其他的JSON 怎么处理？这里好像没有涉及？
    /// </summary>
    static void HandleLuaBundle() {
        string streamDir = Application.dataPath + "/" + AppConst.LuaTempDir;     //E:/TestCode/LuaFramework_UGUI_V2-master/Assets/Lua/    这个是临时创建的文件，不知道有什么用 可能就是为了 转换后缀名。 。byte 最终被出包的也是这里的  XXX.LUA.BYTE 文件；
        Debug.Log("创建临时汇集目录 "+streamDir);
        if (!Directory.Exists(streamDir)) Directory.CreateDirectory(streamDir);  // 好像没有创建这个目录，找不到
      
       
        string[] srcDirs = { CustomSettings.luaDir, CustomSettings.FrameworkPath + "/ToLua/Lua" };  // 这个2个目录 又是本质区别？
        Debug.Log("自定义具体项目lua脚本的目录 "+ srcDirs[0]);   // E:/TestCode/LuaFramework_UGUI_V2-master/Assets/LuaFramework/Lua/  
        Debug.Log("Tolua封装好的工具集或插件 "+ srcDirs[1]);  // E:/TestCode/LuaFramework_UGUI_V2-master/Assets/LuaFramework/ToLua/Lua 比如这里可以换成 xlua？
        for (int i = 0; i < srcDirs.Length; i++) {
            if (AppConst.LuaByteMode) {
                string sourceDir = srcDirs[i];
                string[] files = Directory.GetFiles(sourceDir, "*.lua", SearchOption.AllDirectories);
                int len = sourceDir.Length;

                if (sourceDir[len - 1] == '/' || sourceDir[len - 1] == '\\') {
                    --len;
                }
                for (int j = 0; j < files.Length; j++) {
                    string str = files[j].Remove(0, len);
                    string dest = streamDir + str + ".bytes";
                    string dir = Path.GetDirectoryName(dest);
                    Directory.CreateDirectory(dir);
                    EncodeLuaFile(files[j], dest);
                }    
            } else {
                ToLuaMenu.CopyLuaBytesFiles(srcDirs[i], streamDir); // 谁拷贝到谁？  把前面的目录下的所有.lua文件 拷贝到 后面的目录下 并且加上 .byte; 把原来2个目录下.lua文件拷贝到 工程\lua\目录下
            }
        }
        string[] dirs = Directory.GetDirectories(streamDir, "*", SearchOption.AllDirectories); // 然后继续在新路径下 遍历所有的目录
        for (int i = 0; i < dirs.Length; i++) {
            Debug.Log(dirs[i]);      //比如： E:/TestCode/LuaFramework_UGUI_V2-master/Assets/Lua/3rd
            string name = dirs[i].Replace(streamDir, string.Empty); // 路径名字如果有空字符 处理掉，当然命名的时候不建议带有空字符的
            name = name.Replace('\\', '_').Replace('/', '_'); // 处理所有的 目录分隔符 变成 _; 这个方便后面 解析导入lua ； 所以不建议新建命名lua 文件的时候 使用下划线 _
            name = "lua/lua_" + name.ToLower() + AppConst.ExtName; // 起个打包的报名字，到时候方便 反向解析回来 如 "lua/lua_XXXX_YYY_BB.lua.bytes.unity3d"

            string path = "Assets" + dirs[i].Replace(Application.dataPath, "");
            Debug.Log("相对路劲path "+path);  // 得到的是相对路径 比如  Assets/Lua/3rd
            Debug.Log("可能出的 assetBundleName "+name);   //   name lua/lua_3rd.unity3d
            AddBuildMap(name, "*.bytes", path);  // 最终 搞到 bundle 打包的是  XXX.LUA.BYTE 文件； 
        }
       AddBuildMap("lua/lua" + AppConst.ExtName, "*.bytes", "Assets/" + AppConst.LuaTempDir); //  对应的改文件的出包设置

        //-------------------------------处理非Lua文件---------------------------------- 
        string luaPath = AppDataPath + "/StreamingAssets/lua/";
        Debug.Log(luaPath);
        for (int i = 0; i < srcDirs.Length; i++) {
            paths.Clear(); files.Clear();
            string luaDataPath = srcDirs[i].ToLower();
            Recursive(luaDataPath);
            foreach (string f in files) {
                if (f.EndsWith(".meta") || f.EndsWith(".lua")) continue; //  遇到这个两类型的文件 跳过；
                string newfile = f.Replace(luaDataPath, ""); // 去掉目录得到 文件名
                string path = Path.GetDirectoryName(luaPath + newfile);
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        
                string destfile = path + "/" + Path.GetFileName(f); /// 非lua文件直接拷贝 到 出包文件的对应目录，目前 是定义到  luaPath = AppDataPath + "/StreamingAssets/lua/";
                File.Copy(f, destfile, true);
            }
        }
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 处理框架实例包  实例的资源 打包进来 可以借鉴 打 prefab 或者其他格式的 临时包 主要是API AddBuildMap 的调用；想要打包必然要用到这个 ：它会把要打的资源 标记；
    /// </summary>
    static void HandleExampleBundle() {
        string resPath = AppDataPath + "/" + AppConst.AssetDir + "/";
        if (!Directory.Exists(resPath)) Directory.CreateDirectory(resPath);

        AddBuildMap("prompt" + AppConst.ExtName, "*.prefab", "Assets/LuaFramework/Examples/Builds/Prompt"); // 指定出包的扩展名是  .unity3d  指定具体的哪个文件目录比如是Builds/Prompt 要被出包；并且删选下面的 .prefab文件；
            // 最终出包的名字是 prompt.unity3d  他实际上是 Examples/Builds/Prompt 目录下的2个 预制件文件：PromptItem 和 PromptPanel
        AddBuildMap("message" + AppConst.ExtName, "*.prefab", "Assets/LuaFramework/Examples/Builds/Message");

        AddBuildMap("prompt_asset" + AppConst.ExtName, "*.png", "Assets/LuaFramework/Examples/Textures/Prompt");
        AddBuildMap("shared_asset" + AppConst.ExtName, "*.png", "Assets/LuaFramework/Examples/Textures/Shared");
    }

    /// <summary>
    /// 处理Lua文件
    /// </summary>
    static void HandleLuaFile() {
        string resPath = AppDataPath + "/StreamingAssets/";
        string luaPath = resPath + "/lua/";

        //----------复制Lua文件----------------
        if (!Directory.Exists(luaPath)) {
            Directory.CreateDirectory(luaPath); 
        }
        string[] luaPaths = { AppDataPath + "/LuaFramework/lua/", 
                              AppDataPath + "/LuaFramework/Tolua/Lua/" };

        for (int i = 0; i < luaPaths.Length; i++) {
            paths.Clear(); files.Clear();
            string luaDataPath = luaPaths[i].ToLower();
            Recursive(luaDataPath);
            int n = 0;
            foreach (string f in files) {
                if (f.EndsWith(".meta")) continue;
                string newfile = f.Replace(luaDataPath, "");
                string newpath = luaPath + newfile;
                string path = Path.GetDirectoryName(newpath);
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                if (File.Exists(newpath)) {
                    File.Delete(newpath);
                }
                if (AppConst.LuaByteMode) {
                    EncodeLuaFile(f, newpath);
                } else {
                    File.Copy(f, newpath, true);
                }
                UpdateProgress(n++, files.Count, newpath);
            } 
        }
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
    }
     // 创建一个文本用来记录MD5 
    static void BuildFileIndex() {
        string resPath = AppDataPath + "/StreamingAssets/";
        ///----------------------创建文件列表-----------------------
        string newFilePath = resPath + "/files.txt";
        if (File.Exists(newFilePath)) File.Delete(newFilePath);

        paths.Clear(); files.Clear();
        Recursive(resPath);

        FileStream fs = new FileStream(newFilePath, FileMode.CreateNew);
        StreamWriter sw = new StreamWriter(fs);
        for (int i = 0; i < files.Count; i++) {
            string file = files[i];
            string ext = Path.GetExtension(file);
            if (file.EndsWith(".meta") || file.Contains(".DS_Store")) continue; // 扩展名 是这2个 不处理

            string md5 = Util.md5file(file); // 对这个文件进行 MD5 验算
            string value = file.Replace(resPath, string.Empty); //提取文件的相对路径，前面的绝对路径局部删掉 最后有可能是这样子 lua/lua_3rd_luabitop.unity3d|f58fbf8e11c50a7fd468d0fcd44627f5
            sw.WriteLine(value + "|" + md5);
        }
        sw.Close(); fs.Close();
    }

    /// <summary>
    /// 数据目录
    /// </summary>
    static string AppDataPath {
        get { return Application.dataPath.ToLower(); }
    }

    /// <summary>
    /// 遍历目录及其子目录 并且忽略 。meta 文件的处理  全部存入 files 列表中
    /// </summary>
    static void Recursive(string path) {
        string[] names = Directory.GetFiles(path);
        string[] dirs = Directory.GetDirectories(path);
        foreach (string filename in names) {
            string ext = Path.GetExtension(filename); //返回指定路径文件的 扩展名
            if (ext.Equals(".meta")) continue;
            files.Add(filename.Replace('\\', '/')); // 反斜杠 变成 斜杆， 文件系统 变成 网页系统
        }
        foreach (string dir in dirs) {
            paths.Add(dir.Replace('\\', '/'));
            Recursive(dir);
        }
    }

    static void UpdateProgress(int progress, int progressMax, string desc) {
        string title = "Processing...[" + progress + " - " + progressMax + "]";
        float value = (float)progress / (float)progressMax;
        EditorUtility.DisplayProgressBar(title, desc, value);
    }

    public static void EncodeLuaFile(string srcFile, string outFile) {
        if (!srcFile.ToLower().EndsWith(".lua")) {
            File.Copy(srcFile, outFile, true);
            return;
        }
        bool isWin = true; 
        string luaexe = string.Empty;
        string args = string.Empty;
        string exedir = string.Empty;
        string currDir = Directory.GetCurrentDirectory();
        if (Application.platform == RuntimePlatform.WindowsEditor) {
            isWin = true;
            luaexe = "luajit.exe";
            args = "-b -g " + srcFile + " " + outFile;
            exedir = AppDataPath.Replace("assets", "") + "LuaEncoder/luajit/";
        } else if (Application.platform == RuntimePlatform.OSXEditor) {
            isWin = false;
            luaexe = "./luajit";
            args = "-b -g " + srcFile + " " + outFile;
            exedir = AppDataPath.Replace("assets", "") + "LuaEncoder/luajit_mac/";
        }
        Directory.SetCurrentDirectory(exedir);
        ProcessStartInfo info = new ProcessStartInfo();
        info.FileName = luaexe;
        info.Arguments = args;
        info.WindowStyle = ProcessWindowStyle.Hidden;
        info.UseShellExecute = isWin;
        info.ErrorDialog = true;
        Util.Log(info.FileName + " " + info.Arguments);

        Process pro = Process.Start(info);
        pro.WaitForExit();
        Directory.SetCurrentDirectory(currDir);
    }

    [MenuItem("LuaFramework/Build Protobuf-lua-gen File")]
    public static void BuildProtobufFile() {
        if (!AppConst.ExampleMode) {
            UnityEngine.Debug.LogError("若使用编码Protobuf-lua-gen功能，需要自己配置外部环境！！");
            return;
        }
        string dir = AppDataPath + "/Lua/3rd/pblua";
        paths.Clear(); files.Clear(); Recursive(dir);

        string protoc = "d:/protobuf-2.4.1/src/protoc.exe";
        string protoc_gen_dir = "\"d:/protoc-gen-lua/plugin/protoc-gen-lua.bat\"";

        foreach (string f in files) {
            string name = Path.GetFileName(f);
            string ext = Path.GetExtension(f);
            if (!ext.Equals(".proto")) continue;

            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = protoc;
            info.Arguments = " --lua_out=./ --plugin=protoc-gen-lua=" + protoc_gen_dir + " " + name;
            info.WindowStyle = ProcessWindowStyle.Hidden;
            info.UseShellExecute = true;
            info.WorkingDirectory = dir;
            info.ErrorDialog = true;
            Util.Log(info.FileName + " " + info.Arguments);

            Process pro = Process.Start(info);
            pro.WaitForExit();
        }
        AssetDatabase.Refresh();
    }
}