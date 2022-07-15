require "3rd/pblua/login_pb"
require "3rd/pbc/protobuf"

local lpeg = require "lpeg"

local json = require "cjson"
local util = require "3rd/cjson/util"

local sproto = require "3rd/sproto/sproto"
local core = require "sproto.core"
local print_r = require "3rd/sproto/print_r"

require "Logic/LuaClass"
require "Logic/CtrlManager"
require "Common/functions"
require "Controller/PromptCtrl"

--管理器--
Game = {};
local this = Game;

local game; 
local transform;
local gameObject;
local WWW = UnityEngine.WWW;


--- 这里的作用意图是 加载每一个交互界面的 lua逻辑 代码
---- 正常情况下是一个界面名字和对应的lua脚本名字差不多 而且 放到 view下面，当然也可以弄到其他路径
function Game.InitViewPanels()
	for i = 1, #PanelNames do
		require ("View/"..tostring(PanelNames[i])) -- 每一个界面名字都有对应一个 view/pannlexxx.lua 的文件描述吗
	end
end

--初始化完成，发送链接服务器信息--
function Game.OnInitOK()
    AppConst.SocketPort = 2012;
    AppConst.SocketAddress = "127.0.0.1";
    networkMgr:SendConnect(); -- 这个是调用c# 端  NetworkManager.SendConnect() 发起连接请求

    --注册LuaView--
    this.InitViewPanels();
----------------------------------------------------
    this.test_class_func();
    this.test_pblua_func(); -- 测试 protobuf 的用法
    this.test_cjson_func(); --- 测试 json 用法
    this.test_pbc_func(); --- 测试 pbc 是什么玩意
    this.test_lpeg_func();
    this.test_sproto_func();
    coroutine.start(this.test_coroutine); -- 测试 协程用法

    CtrlManager.Init(); --- 这个控制器到底是什么玩意？
    local ctrl = CtrlManager.GetCtrl(CtrlNames.Prompt);
    if ctrl ~= nil and AppConst.ExampleMode == 1 then
        ctrl:Awake();
    end
       
    logWarn('LuaFramework InitOK--->>>');
end

--测试协同--
function Game.test_coroutine()    
    logWarn("1111");
    coroutine.wait(1);	
    logWarn("2222");
	
    local www = WWW("http://bbs.ulua.org/readme.txt"); --- 这个网站明显访问不了
    coroutine.www(www);
    logWarn(www.text);    	
end

--测试sproto--
function Game.test_sproto_func()
    logWarn("test_sproto_func-------->>");
    local sp = sproto.parse [[
    .Person {
        name 0 : string
        id 1 : integer
        email 2 : string

        .PhoneNumber {
            number 0 : string
            type 1 : integer
        }

        phone 3 : *PhoneNumber
    }

    .AddressBook {
        person 0 : *Person(id)
        others 1 : *Person
    }
    ]]

    local ab = {
        person = {
            [10000] = {
                name = "Alice",
                id = 10000,
                phone = {
                    { number = "123456789" , type = 1 },
                    { number = "87654321" , type = 2 },
                }
            },
            [20000] = {
                name = "Bob",
                id = 20000,
                phone = {
                    { number = "01234567890" , type = 3 },
                }
            }
        },
        others = {
            {
                name = "Carol",
                id = 30000,
                phone = {
                    { number = "9876543210" },
                }
            },
        }
    }
    local code = sp:encode("AddressBook", ab)
    local addr = sp:decode("AddressBook", code)
    print_r(addr)
end

--测试lpeg--
function Game.test_lpeg_func()
	logWarn("test_lpeg_func-------->>");
	-- matches a word followed by end-of-string
	local p = lpeg.R"az"^1 * -1

	print(p:match("hello"))        --> 6
	print(lpeg.match(p, "hello"))  --> 6
	print(p:match("1 hello"))      --> nil
end

--测试lua类--
function Game.test_class_func()
    LuaClass:New(10, 20):test();-- 随便整理的，玩下 luaClass构造lua 类表，以及实例方法；
end

--测试pblua-- 这应该是测试 lua protobuf 的用法， 这里是反序列化 包成 二进制
function Game.test_pblua_func()
    local login = login_pb.LoginRequest();
    login.id = 2000;
    login.name = 'game';
    login.email = 'jarjin@163.com';
    
    local msg = login:SerializeToString();
    LuaHelper.OnCallLuaFunc(msg, this.OnPbluaCall); --- 这句是直接 搞？ 序列和马上反序列？
end

--pblua callback-- -- 这里是回调，监听返回的东西 序列化解析，
function Game.OnPbluaCall(data)
    local msg = login_pb.LoginRequest();
    msg:ParseFromString(data);
    print(msg);
    print(msg.id..' '..msg.name);
end

--测试pbc--
function Game.test_pbc_func()
    local path = Util.DataPath.."lua/3rd/pbc/addressbook.pb";
    log('io.open--->>>'..path);

    local addr = io.open(path, "rb")
    local buffer = addr:read "*a"
    addr:close()
    protobuf.register(buffer)

    local addressbook = {
        name = "Alice",
        id = 12345,
        phone = {
            { number = "1301234567" },
            { number = "87654321", type = "WORK" },
        }
    }
    local code = protobuf.encode("tutorial.Person", addressbook)
    LuaHelper.OnCallLuaFunc(code, this.OnPbcCall)
end

--pbc callback--
function Game.OnPbcCall(data)
    local path = Util.DataPath.."lua/3rd/pbc/addressbook.pb";

    local addr = io.open(path, "rb")
    local buffer = addr:read "*a"
    addr:close()
    protobuf.register(buffer)
    local decode = protobuf.decode("tutorial.Person" , data)

    print(decode.name)
    print(decode.id)
    for _,v in ipairs(decode.phone) do
        print("\t"..v.number, v.type)
    end
end

--测试cjson--
function Game.test_cjson_func()
    local path = Util.DataPath.."lua/3rd/cjson/example2.json";
    local text = util.file_load(path);
    LuaHelper.OnJsonCallFunc(text, this.OnJsonCall);
end

--cjson callback--
function Game.OnJsonCall(data)
    local obj = json.decode(data);
    print(obj['menu']['id']);
end

--销毁--
function Game.OnDestroy()
	--logWarn('OnDestroy--->>>');
end
