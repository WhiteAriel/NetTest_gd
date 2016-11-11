using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;//须引用 ~Framework\v3.5\System.Web.Extensions.dll  
using System.Data;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using NetLog;


namespace ParseandBuildJson                    //该命名空间用于解析终端任务下发数据json与生成任务执行返回数据json格式
{

    //public class CommandJson
    //{
    //    public int CommandType;
    //    public string CommandData;
    //}

    public class  ActionJson
    {
        public string Id;
        public string Action;
    }

    public class TaskJson
    {
        public int Recycle;
        public string ServerIp;
        public List<AttributeJson> Task=null;
    }

    public class AttributeJson
    {
        public string Id;
        public string Type;
        public string Url;
        public string BatchNo;

        public AttributeJson(string a, string b, string c,string d)
        {
            this.Id = a;
            this.Type = b;
            this.Url = c;
            this.BatchNo = d;
        }

    }

    public class RetJson
    {
        public string Id;
        public int ErrorCode;
        public string Message;
        public RetJson(string Id, int ErrorCode, string Message)
        {
            this.Id = Id;
            this.ErrorCode = ErrorCode;
            this.Message = Message;
        }

    }



    public class OperateJson
    {


        //jsonstr标准格式{"CommandType":0任务/1控制，"CommandData":{Recycle:0/1,ServerIp:192.168.1.1:8088,\
        // Task:[{Id:xxxx,Type:xxxxx,Url:xxxxxxxx },{………},{…………},{…………} ]}任务/{"Id:xxx","Action":0开始/1暂停}控制;}
        /*
         *return value:0(json串为空或任务为空);1(normal);3(Exception)
         *             220(control start);221(control stop);300(unknown action)
         * 
         */
        static public int ParseJson(string jsonstr, ref List<AttributeJson> taskList, ref int recycle, ref string serverIp)  //终端任务下发数据解析json格式，返回0则数据为空，返回1则正常，返回2则异常
        {
            if (jsonstr == "")
                return 0;                                 //数据为空
            else
            {
                try
                {
                    //先解析外层的CommandType和CommandData
                    JObject joCommand = (JObject)JsonConvert.DeserializeObject(jsonstr);
                    if (joCommand["CommandType"].ToString()=="0")  //task
                    {
                        TaskJson taskJson = JsonConvert.DeserializeObject<TaskJson>(joCommand["CommandData"].ToString());
                           if (taskJson.Task==null)
                               return 0;                     //任务为空
                           else
                            {
                               int count = taskJson.Task.Count();
                               recycle = taskJson.Recycle;
                               serverIp = taskJson.ServerIp;
                               for (int i = 0; i < count; i++)
                                   taskList.Insert(i,taskJson.Task[i]);
                            }
                         
                    }
                    else if (joCommand["CommandType"].ToString() == "1")  //control
                    {
                        ActionJson actionJson = JsonConvert.DeserializeObject<ActionJson>(joCommand["CommandData"].ToString());
                        if (actionJson.Action == "0")  //start
                            return 220;
                        else if (actionJson.Action == "1")  //stop
                            return 221;
                        else
                            return 300;   //unknown action
                    }
                }
                catch (Exception ex)
                {
                   Log.Console(Environment.StackTrace,ex); Log.Warn(Environment.StackTrace,ex);
                    return 3;   //出现异常，解析的数据非Json格式
                }
                return 1;  //正常格式
            }
        }

        static public string BuildJson(string Id, int ErrorCode, string Message)   //任务执行返回数据json格式
        {
            RetJson retJson = new RetJson(Id, ErrorCode, Message);
            return JsonConvert.SerializeObject(retJson);;
        }
    }


}
