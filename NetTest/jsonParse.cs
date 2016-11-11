using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;//������ ~Framework\v3.5\System.Web.Extensions.dll  
using System.Data;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using NetLog;


namespace ParseandBuildJson                    //�������ռ����ڽ����ն������·�����json����������ִ�з�������json��ʽ
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


        //jsonstr��׼��ʽ{"CommandType":0����/1���ƣ�"CommandData":{Recycle:0/1,ServerIp:192.168.1.1:8088,\
        // Task:[{Id:xxxx,Type:xxxxx,Url:xxxxxxxx },{������},{��������},{��������} ]}����/{"Id:xxx","Action":0��ʼ/1��ͣ}����;}
        /*
         *return value:0(json��Ϊ�ջ�����Ϊ��);1(normal);3(Exception)
         *             220(control start);221(control stop);300(unknown action)
         * 
         */
        static public int ParseJson(string jsonstr, ref List<AttributeJson> taskList, ref int recycle, ref string serverIp)  //�ն������·����ݽ���json��ʽ������0������Ϊ�գ�����1������������2���쳣
        {
            if (jsonstr == "")
                return 0;                                 //����Ϊ��
            else
            {
                try
                {
                    //�Ƚ�������CommandType��CommandData
                    JObject joCommand = (JObject)JsonConvert.DeserializeObject(jsonstr);
                    if (joCommand["CommandType"].ToString()=="0")  //task
                    {
                        TaskJson taskJson = JsonConvert.DeserializeObject<TaskJson>(joCommand["CommandData"].ToString());
                           if (taskJson.Task==null)
                               return 0;                     //����Ϊ��
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
                    return 3;   //�����쳣�����������ݷ�Json��ʽ
                }
                return 1;  //������ʽ
            }
        }

        static public string BuildJson(string Id, int ErrorCode, string Message)   //����ִ�з�������json��ʽ
        {
            RetJson retJson = new RetJson(Id, ErrorCode, Message);
            return JsonConvert.SerializeObject(retJson);;
        }
    }


}
