using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using NetLog;

namespace MultiMySQL
{
    public struct videoPara
    {
        public int still;
        public int blur;
        public int skip;
        public int black;
        public int definition;
        public int brightness;
        public int chroma;
        public int saturation;
        public int contraction;
        public int dev;
        public int entro;
        public double block;
        public double highEnerge;
        public double score;

        public videoPara(int still,int blur,int skip,int black,int definition,int brightness,int chroma,int saturation,int contraction,int dev,int entro,double block,double highEnerge,double score)
        {
            this.still=still;
            this.blur=blur;
            this.skip=skip;
            this.black=black;
            this.definition=definition;
            this.brightness=brightness;
            this.chroma=chroma;
            this.saturation=saturation;
            this.contraction=contraction;
            this.dev=dev;
            this.entro=entro;
            this.block=block;
            this.highEnerge=highEnerge;
            this.score = score;
        }
    }
    class TaskItems
    {
        public string taskId { get; set; }
        public string taskType { get; set; }
        public int taskUrlType { get; set; }
        public string taskUrl { get; set; }
        public string serverIp { get; set; }
        public string actionStatus { get; set; }
        public string remarks { get; set; }
    }
    class MySQLInterface
    {
        private MySqlConnection mycon;
        private string conString;
        private object mysqlFilterLock = new object();
        private object mysqlUpdateLock = new object();
        private string sharedKeys = "TimeStamp#SearchIndex#Guid#TaskId#TaskType#SyncStatus#SyncTime";
        public string errorInfo { get; set; }

        //构造函数时就完成连接MySQL和创造数据库
        public MySQLInterface(string server, string user, string password, string dbName,string port)
        {
            if (string.IsNullOrEmpty(port))
                port = "3306";
            this.conString = "server=" + server + ";User Id=" + user + ";password=" + password + ";Port=" + port+";";
            this.mycon = new MySqlConnection(conString);
            this.conString = "server=" + server + ";User Id=" + user + ";password=" + password + ";Database=" + dbName + ";Port=" + port + ";";
            this.errorInfo = null;
        }

        ~MySQLInterface()
        {
            MySQLClose();
        }

        public bool MysqlInit(string dbname)
        {
            if (!MySQLOpen())
                return false;
            if (!CreatDB(dbname))
                return false;
            return true;
        }
        private bool MySQLOpen()
        {
            try
            {
                mycon.Open();
            }
            catch (Exception ex)
            {
                errorInfo += "MySQL连接失败" + ex.Message;
                Log.Console(Environment.StackTrace, ex); Log.Warn(Environment.StackTrace, ex);
                return false;
            }
            return true;
        }


        public void MySQLClose()
        {
            try
            {
                mycon.Close();
            }
            catch (Exception ex)
            {
                errorInfo += " MySQL断开失败" + ex.Message;
                Log.Console(Environment.StackTrace, ex); Log.Warn(Environment.StackTrace, ex);
            }
        }



        //创造数据库
        private bool CreatDB(string DBname)
        {
            try
            {
                string DBhead = "CREATE DATABASE IF NOT EXISTS   ";
                string creatDB = DBhead + DBname + " CHARACTER SET utf8;";
                MySqlCommand creatdatabase = new MySqlCommand(creatDB, mycon);
                creatdatabase.ExecuteNonQuery();
                string usedb = "use " + DBname;
                MySqlCommand UseDB = new MySqlCommand(usedb, mycon);
                UseDB.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                errorInfo += " 创建MySQL数据库失败" + ex.Message;
                Log.Console(Environment.StackTrace, ex); Log.Warn(Environment.StackTrace, ex);
                return false;
            }
            return true;
        }



        //存入txt时，创造相关表
        public bool CreatHttpAnalysis()
        {
            try
            {
                string creatHttp = "create table IF NOT EXISTS HttpAnalysis(TimeStamp VARCHAR(20) not null,SearchIndex INT auto_increment Unique,Guid VARCHAR(36) primary key not null,TaskId VARCHAR(36) not null,TaskType VARCHAR(20) not null,SyncStatus INT not null, SyncTime VARCHAR(20),HttpClientIp VARCHAR(20) not null,HttpMethod VARCHAR(10) not null,HttpUrl VARCHAR(2083)not null,HttpServerIp VARCHAR(20) not null,HttpVersion VARCHAR(10) not null,HttpResponseDelay VARCHAR(10) not null);";
                //MySqlCommand creattable = new MySqlCommand(creatHttp, mycon);
                //creattable.ExecuteNonQuery();
                MySqlHelper.ExecuteNonQuery(conString, creatHttp);
            }
            catch (Exception ex)
            {
                errorInfo += " 创建Http表失败" + ex.Message;
                Log.Console(Environment.StackTrace, ex); Log.Warn(Environment.StackTrace, ex);
                return false;
            }
            return true;
        }

        public bool CreatDNSAnalysis()
        {
            try
            {
                string creatDns = "create table IF NOT EXISTS DNSAnalysis(TimeStamp VARCHAR(20) not null,SearchIndex INT auto_increment Unique,Guid VARCHAR(36) primary key not null,TaskId VARCHAR(36) not null,TaskType VARCHAR(20) not null, SyncStatus INT not null, SyncTime VARCHAR(20),DnsClientIp VARCHAR(20) not null,DnsClientPort VARCHAR(10) not null,DnsServerIp VARCHAR(20)not null,DnsServerPort VARCHAR(10) not null,DnsDomainName VARCHAR(64) not null,DnsReturnCode VARCHAR(6) not null,DnsResponseIp VARCHAR(20)not null,DnsResponseDelay VARCHAR(10) not null);";
                MySqlHelper.ExecuteNonQuery(conString, creatDns);
            }
            catch (Exception ex)
            {
                errorInfo += " 创建Dns表失败" + ex.Message;
                Log.Console(Environment.StackTrace, ex); Log.Warn(Environment.StackTrace, ex);
                return false;
            }
            return true;
        }

        public bool CreatInOutAnalysis()
        {
            try
            {
                string creatInOut = "create table IF NOT EXISTS InOutAnalysis(TimeStamp VARCHAR(20) not null,SearchIndex INT auto_increment Unique,Guid VARCHAR(36) primary key not null,TaskId VARCHAR(36) not null,TaskType VARCHAR(20) not null, SyncStatus INT not null, SyncTime VARCHAR(20),TimeInterval VARCHAR(20) not null,Traffic VARCHAR(20) not null);";
                MySqlHelper.ExecuteNonQuery(conString, creatInOut);
            }
            catch (Exception ex)
            {
                errorInfo += " 创建MySQL表失败" + ex.Message;
                Log.Console(Environment.StackTrace, ex); 
                Log.Warn(Environment.StackTrace, ex);
                return false;
            }
            return true;
        }

        public bool CreatFrameLengthAnalysis()
        {
            try
            {
                string creatFrameLen = "create table IF NOT EXISTS FrameLengthAnalysis(TimeStamp VARCHAR(20) not null,SearchIndex INT auto_increment Unique,Guid VARCHAR(36) primary key not null,TaskId VARCHAR(36) not null,TaskType VARCHAR(20) not null, SyncStatus INT not null, SyncTime VARCHAR(20),FrameLength VARCHAR(20) not null,FrameNum VARCHAR(10) not null,FramePercent VARCHAR(20) not null);";
                MySqlHelper.ExecuteNonQuery(conString, creatFrameLen);
            }
            catch (Exception ex)
            {
                errorInfo += " 创建MySQL表失败" + ex.Message;
                Log.Console(Environment.StackTrace, ex); Log.Warn(Environment.StackTrace, ex);
                return false;
            }
            return true;
        }

        public bool CreatDelayJitter()
        {
            try
            {

                string creatDelayJitter = "create table IF NOT EXISTS DelayJitter(TimeStamp VARCHAR(20) not null,SearchIndex INT auto_increment Unique,Guid VARCHAR(36) primary key not null,TaskId VARCHAR(36) not null,TaskType VARCHAR(20) not null, SyncStatus INT not null, SyncTime VARCHAR(20),TcpIndex VARCHAR(10) not null,TcpSegmentIndex  VARCHAR(10) not null,Delay VARCHAR(10) not null,Jitter VARCHAR(10) not null);";
                //MySqlCommand creattable = new MySqlCommand(DelayJitter, mycon);
                //creattable.ExecuteNonQuery();
                MySqlHelper.ExecuteNonQuery(conString, creatDelayJitter);
            }
            catch (Exception ex)
            {
                errorInfo += " 创建MySQL表失败" + ex.Message;
                Log.Console(Environment.StackTrace, ex); Log.Warn(Environment.StackTrace, ex);
                return false;
            }
            return true;
        }

        //存入视频参数
        public bool CreatVideoPara()
        {
            try
            {
                string creatVideoPara = "create table IF NOT EXISTS VideoPara(TimeStamp VARCHAR(20) not null,SearchIndex INT auto_increment Unique,Guid VARCHAR(36) primary key not null,TaskId VARCHAR(36) not null,TaskType VARCHAR(20) not null, SyncStatus INT not null, SyncTime VARCHAR(20),Still INT not null,Blur INT not null,Skip INT not null,Black INT not null,Definition INT not null,Brightness INT not null,Chroma INT not null,Saturation INT not null,Contraction INT not null,Dev INT not null,Entro INT not null,Block DOUBLE not null,HighEnerge DOUBLE not null,Score DOUBLE not null);";      //,Clarity DOUBLE not null,Brightness DOUBLE not null,Chroma DOUBLE not null,Saturation DOUBLE not null ,Contrast DOUBLE not null ,ScreenStatic DOUBLE not null ,ScreenJump DOUBLE not null ,ScreenFuzzy DOUBLE not null
                //MySqlCommand creattable = new MySqlCommand(creatVideoPara, mycon);
                //creattable.ExecuteNonQuery();
                MySqlHelper.ExecuteNonQuery(conString, creatVideoPara);
            }
            catch (Exception ex)
            {
                errorInfo += " 创建MySQL表失败" + ex.Message;
                Log.Console(Environment.StackTrace, ex); Log.Warn(Environment.StackTrace, ex);
                return false;
            }
            return true;
        }

        public bool CreatTestReport()
        {
            try
            {
                string creatTestReport = "create table IF NOT EXISTS TestReport(TimeStamp VARCHAR(20) not null,SearchIndex INT auto_increment Unique,Guid VARCHAR(36) primary key not null,TaskId VARCHAR(36) not null,TaskType VARCHAR(20) not null, SyncStatus INT not null, SyncTime VARCHAR(20),TestKey VARCHAR(20)not null ,TestValue VARCHAR(20) not null);";
                //MySqlCommand creattable = new MySqlCommand(creatTestReport, mycon);
                //creattable.ExecuteNonQuery();
                MySqlHelper.ExecuteNonQuery(conString, creatTestReport);
            }
            catch (Exception ex)
            {
                errorInfo += " 创建MySQL表失败" + ex.Message;
                Log.Console(Environment.StackTrace, ex); Log.Warn(Environment.StackTrace, ex);
                return false;
            }
            return true;
        }

        public bool CreatTaskList()
        {
            try
            {
                string creatTaskList = "create table IF NOT EXISTS TaskList(TaskIndex INT  primary key auto_increment,BatchNo VARCHAR(20) not null,TaskId VARCHAR(36) not null,TaskType VARCHAR(10) not null,TaskUrlType INT not null,TaskUrl VARCHAR(2083) not null,ServerIp VARCHAR(20) not null,SyncStatus VARCHAR(5) not null,ActionStatus VARCHAR(5) not null,Remarks VARCHAR(100));";
                //MySqlCommand creattable = new MySqlCommand(creatTaskList, mycon);//auto_increment
                //creattable.ExecuteNonQuery();
                MySqlHelper.ExecuteNonQuery(conString, creatTaskList);
            }
            catch (Exception ex)
            {
                errorInfo += " 创建MySQL表失败" + ex.Message;
                Log.Console(Environment.StackTrace, ex); Log.Warn(Environment.StackTrace, ex);
                return false;
            }
            return true;
        }
        /**************************************************************************************************************** 
         *此函数将指定路径的txt内容导入到datatable（datatable中还包括用户输入的键、值）
         *并根据txt的内容为MySQL增加两列键
         ******************************************************************************************************************/
        private DataTable Txt_DataTable(string txtpath, string idAndType)
        {
            try
            {
                string[] key = sharedKeys.Split(new char[] { '#' });
                string[] value = idAndType.Split(new char[] { '#' });
                FileStream aFile = new FileStream(txtpath, FileMode.Open);
                StreamReader objReader = new StreamReader(aFile, System.Text.Encoding.Default);
                String line = objReader.ReadLine();
                string[] cols = line.Split(new char[1] { '\t' });

                string strLine = objReader.ReadLine();
                /* 按照传入的key值作为键，生成datatable，它的后面的键为txt读入的内容*******************************************/
                DataTable dt = new DataTable();
                for (int i = 0; i < key.Length; i++)
                    dt.Columns.Add(new DataColumn(key[i], Type.GetType("System.String")));
                for (int j = 1; j < cols.Length; j++)
                    dt.Columns.Add(new DataColumn(cols[j], Type.GetType("System.String")));//.Int32
                /*遍历txt，将txt中的内容导入到datatable中********************************************************************** */
                //int indexnum = 0;
                while (strLine != null)
                {

                    strLine = strLine.Replace(",", "_");
                    string[] str = strLine.Split(new Char[] { '\t' });
                    DataRow dr = dt.NewRow();
                    dr[key[0]] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    //dr[key[1]] = ++indexnum;
                    dr[key[2]] = System.Guid.NewGuid();
                    dr[key[3]] = value[0];  //任务id
                    dr[key[4]] = value[1];  //类别
                    dr[key[5]] = 0;  //SyncStatus默认为0

                    for (int k = 1; k < cols.Length; k++)
                        dr[cols[k]] = str[k];
                    dt.Rows.Add(dr);
                    //}
                    strLine = objReader.ReadLine();

                }
                objReader.Close();
                if (dt.Rows.Count > 0)
                {
                    return dt;
                }
                return null;
            }
            catch (Exception ex)
            {
                errorInfo += " txt写入数据表失败" + ex.Message;
                Log.Console(Environment.StackTrace, ex); Log.Warn(Environment.StackTrace, ex);
                return null;
            }
        }
        /**************************************************************************************************************** 
         *此函数将指定指定的datatable保存为CSV文件
         ******************************************************************************************************************/
        private string DataTable_CSV(DataTable dtable)
        {
            try
            {
                string CVSFilePath = DateTime.Now.Ticks.ToString() + ".csv";
                StreamWriter sw = new StreamWriter(CVSFilePath, false);
                int icolcount = dtable.Columns.Count;
                foreach (DataRow drow in dtable.Rows)
                {
                    for (int i = 0; i < icolcount; i++)
                    {
                        if (!Convert.IsDBNull(drow[i]))
                        {
                            sw.Write(drow[i].ToString());
                        }
                        if (i < icolcount - 1)
                        {
                            sw.Write(",");
                        }
                    }
                    sw.Write(sw.NewLine);
                }
                sw.Close();
                sw.Dispose();
                return CVSFilePath;
            }
            catch (Exception ex)
            {
                errorInfo += " 数据表生成csv文件失败" + ex.Message;
                Log.Console(Environment.StackTrace, ex); Log.Warn(Environment.StackTrace, ex);
                return null;
            }

        }
        /**************************************************************************************************************** 
        *此函数将datatable导入到MySQL
        ******************************************************************************************************************/
        private bool CSV_MySQL(string CVSFilePath, string tablename)
        {
            try
            {
                MySqlBulkLoader bcp1 = new MySqlBulkLoader(mycon);
                bcp1.TableName = tablename; //Create ProductOrder table into MYSQL database...
                bcp1.FieldTerminator = ",";
                bcp1.LineTerminator = "\r\n";
                bcp1.FileName = CVSFilePath;
                bcp1.NumberOfLinesToSkip = 0;
                bcp1.Load();
                File.Delete(CVSFilePath);
            }
            catch (Exception ex)
            {
                errorInfo += " datatable导入到MySQL失败" + ex.Message;
                Log.Console(Environment.StackTrace, ex); Log.Warn(Environment.StackTrace, ex);
                return false;
            }
            return true;
        }

        //txt批量导入数据库
        public bool TxTInsertMySQL(string tablename, string idAndType, string txtPath)
        {
            DataTable DT = Txt_DataTable(txtPath, idAndType);
            if (DT == null)
                return false;
            string CVSFilePath = DataTable_CSV(DT);
            if (CVSFilePath == null)
                return false;
            if (!CSV_MySQL(CVSFilePath, tablename))
                return false;
            return true;
        }

        //插入视频播放时回传的清晰度等单条记录
        public bool VideoParaInsertMySQL(string idAndType, videoPara vp)
        {
            try
            {
                string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string guid = System.Guid.NewGuid().ToString();
                string[] value = idAndType.Split(new char[] { '#' });
                if (value.Length != 2)
                {
                    errorInfo = errorInfo + " Input idandtype fail!";
                    return false;
                }
                //string sqlInsert = "insert into VideoPara(TimeStamp,Guid,TaskId,TaskType,SyncStatus,Clarity,Brightness,Chroma,Saturation,Contrast,ScreenStatic,ScreenJump,ScreenFuzzy)" + " values('" + ts + "','" + guid + "','" + value[0] + "','" + value[1] + "'," + 0 + "," + vp.clarity + "," + vp.brightness + "," + vp.Chroma + "," + vp.saturation + "," + vp.Contrast + "," + vp.screenstatic + "," + vp.screenjump + "," + vp.screenfuzzy + ")";
                string sqlInsert = "insert into VideoPara(TimeStamp,Guid,TaskId,TaskType,SyncStatus,Still,Blur,Skip,Black,Definition,Brightness,Chroma,Saturation,Contraction,Dev,Entro,Block,HighEnerge,Score)" + " values('" + ts + "','" + guid + "','" + value[0] + "','" + value[1] + "'," + 0 + "," + vp.still + "," + vp.blur + "," + vp.skip + "," + vp.black + "," + vp.definition + "," + vp.brightness + "," + vp.chroma + "," + vp.saturation + "," + vp.contraction + "," + vp.dev + "," + vp.entro + "," + vp.block + "," + vp.highEnerge+","+vp.score+ ")";
                    MySqlHelper.ExecuteNonQuery(conString, sqlInsert);
            }
            catch (Exception ex)
            {
                errorInfo = errorInfo + " 数据插入表失败" + ex.Message;
                Log.Console(Environment.StackTrace, ex); Log.Warn(Environment.StackTrace, ex);
                return false;
            }
            return true;
        }


        //插入单条任务列表
        public bool TaskListInsertMySQL(string batchnoIdTypeRealUrlIp)
        {
            try
            {
                string[] value = batchnoIdTypeRealUrlIp.Split(new char[] { '#' });
                if (value.Length != 6)
                {
                    errorInfo = errorInfo + " Input values fail!";
                    return false;
                }
                //1#22c80fcd7c-f010-4fd9-b8a4-d5207f980642#Video#http://data.vod.itc.cn/?rb=1&p.mp4#192.168.50.120:16201
                string sqlInsert = "insert into TaskList(BatchNo,TaskId,TaskType,TaskUrlType,TaskUrl,ServerIp,SyncStatus,ActionStatus)" + " values(" + "'" + value[0] + "','" + value[1] + "','" + value[2] + "','" + value[3] + "','" + value[4] + "','" + value[5] + "','-1','1')";
                //MySqlCommand Inserttable = new MySqlCommand(sqlInsert, mycon);
                //Inserttable.ExecuteNonQuery();
                MySqlHelper.ExecuteNonQuery(conString, sqlInsert);
            }
            catch (Exception ex)
            {
                errorInfo = errorInfo + " 数据插入表失败" + ex.Message;
                Log.Console(Environment.StackTrace, ex); Log.Warn(Environment.StackTrace, ex);
                return false;
            }
            return true;
        }

        public List<TaskItems> TaskListFilter(string filterFactor)
        {
            //string sqlFilter="select TaskId,TaskType,TaskUrl,ServerIp from TaskList where username='$username'"

            try
            {
                lock (mysqlFilterLock)
                {
                    string selec = "select TaskId,TaskType,TaskUrlType,TaskUrl,ServerIp,ActionStatus,Remarks from  TaskList where " + filterFactor;//limit 0,2;
                    //MySqlCommand tablefilter = new MySqlCommand(selec, mycon);//auto_increment
                    //MySqlDataReader re = tablefilter.ExecuteReader();
                    DataSet dataSet = MySqlHelper.ExecuteDataset(conString, selec);
                    DataTable dtResult = null;
                    if (dataSet != null && dataSet.Tables.Count > 0)
                        dtResult = dataSet.Tables[0];

                    List<TaskItems> taskItemsList = new List<TaskItems>();
                    foreach (DataRow drow in dtResult.Rows)
                    {
                        if (dtResult.Columns.Count == 7)
                        {
                            TaskItems taskItems = new TaskItems();
                            taskItems.taskId = drow[0].ToString();
                            taskItems.taskType = drow[1].ToString();
                            taskItems.taskUrlType = Int32.Parse(drow[2].ToString());
                            taskItems.taskUrl = drow[3].ToString();
                            taskItems.serverIp = drow[4].ToString();
                            taskItems.actionStatus = drow[5].ToString();
                            taskItems.remarks = drow[6].ToString();
                            taskItemsList.Add(taskItems);
                        }
                    }
                    return taskItemsList;
                }
            }
            catch (Exception ex)
            {
                errorInfo = errorInfo + " 数据插入表失败" + ex.Message;
                Log.Console(Environment.StackTrace, ex); Log.Warn(Environment.StackTrace, ex);
                Console.WriteLine("TaskListFilter");
                return null;
            }
        }

        public bool UpdateTaskListColumn(string column, string value, string filterSQL)
        {
            try
            {
                lock (mysqlUpdateLock)
                {
                    //update table1 set field1=value1 where 范围
                    string updateSQL = "update TaskList set " + column + "='" + value + "' where " + filterSQL;//limit 0,2";
                    //MySqlCommand updSQL = new MySqlCommand(updateSQL, mycon);//auto_increment
                    //updSQL.ExecuteNonQuery();
                    MySqlHelper.ExecuteNonQuery(conString, updateSQL);
                    return true;
                }

            }
            catch (Exception ex)
            {
                errorInfo = errorInfo + " 数据插入表失败" + ex.Message;
                Log.Console(Environment.StackTrace, ex); Log.Warn(Environment.StackTrace, ex);
                Console.WriteLine("UpdateTaskListColumn");
                return false;
            }
        }

        //public bool WrongInfInsertTaskList(string wrongInf)
        //{
        //    try
        //    {
               
        //        //1#22c80fcd7c-f010-4fd9-b8a4-d5207f980642#Video#http://data.vod.itc.cn/?rb=1&p.mp4#192.168.50.120:16201
        //        string sqlInsert = "insert into TaskList(Remarks)" + " values(" + "'" + wrongInf + "'" + ")";
        //        //MySqlCommand Inserttable = new MySqlCommand(sqlInsert, mycon);
        //        //Inserttable.ExecuteNonQuery();
        //        MySqlHelper.ExecuteNonQuery(conString, sqlInsert);
        //    }
        //    catch (Exception ex)
        //    {
        //        errorInfo = errorInfo + " 数据插入表失败" + ex.Message;
        //        Log.Console(Environment.StackTrace, ex); Log.Warn(Environment.StackTrace, ex);
        //        return false;
        //    }
        //    return true;
        //}

    }

}