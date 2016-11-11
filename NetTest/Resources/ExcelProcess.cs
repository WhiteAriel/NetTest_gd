/*
 * 设计说明：用于创建、写、读Excel的类
 * 注意事项：使用之前，必须为项目添加Excel的COM组件；方法：项目 -> 添加引用 -> com -> Microsoft Excel Object 12.0 Object Library -> 确定
 *           版本可能有点不一样
 * 程序问题：如果要不停的打开Excel、读或写，然后保存的话，会消耗很多的资源，因为这里采取的是开启进程的方式。
 *           建议把数据采用数组的形式写入Excel，这样可以大大提高效率。
 * 设计时间：2011.07.18
 * 设计作者：王辉
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection; // 引用这个才能使用Missing字段 
//using Excel = Microsoft.Office.Interop.Excel;
using System.Collections;
namespace NetTest
{
    class ExcelProcess
    {
        private string filePath = null;     //文件地址
        Excel.Application eApplication;     //Excel的操作对象
        Excel.Workbook eWorkbook;           //Excel工作区间
        Excel.Worksheet eSheet;             //指定要操作的sheet
        object nothing = System.Reflection.Missing.Value;

        /// <summary>
        /// 构造函数，输入Excel文件路径
        /// </summary>
        /// <param name="path"></param>
        public ExcelProcess(string path)
        {
            this.filePath = path;
        }
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ExcelProcess()
        {
        }
        /// <summary>
        /// 创建一个新的Excel文件，并释放操作
        /// </summary>
        public void createExcel()
        {
            this.eApplication = new Excel.ApplicationClass();
            this.eWorkbook = this.eApplication.Workbooks.Add(true);
            this.eApplication.Visible = false;
            this.eWorkbook.SaveAs(this.filePath, nothing, nothing,
               nothing, nothing, nothing, Excel.XlSaveAsAccessMode.xlShared,
               nothing, nothing, nothing, nothing, nothing);
            this.eApplication.Quit();
        }
        /// <summary>
        /// 打开Excel文件以操作，打开成功的话放回true，否则返回false
        /// </summary>
        public bool openExcel()
        {
            try
            {
                this.eApplication = new Excel.ApplicationClass();
                this.eApplication.Visible = false;
                this.eWorkbook = this.eApplication.Workbooks._Open(this.filePath, nothing, nothing, nothing, nothing,
                    nothing, nothing, nothing, nothing, nothing, nothing, nothing, nothing);//打开Excel文件
                this.eSheet = (Excel.Worksheet)eWorkbook.Sheets[1];//默认对第一个sheet进行操作
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// 将数据写入指定位置，x与y代表实际的行数与列数
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        public void writeExcel(int x, int y, object value)
        {
            Excel.Range rangeData = (Excel.Range)eSheet.Cells[x, y];
            rangeData.Value2 = value;
        }
        /// <summary>
        /// 保存文件并释放资源
        /// </summary>
        public void saveExcel()
        {
            this.eWorkbook.Save();
            this.eSheet = null;
            this.eWorkbook = null;
            this.eApplication.Quit();//退出应用程序
            this.eApplication = null;
        }
        /// <summary>
        /// 返回指定位置的值
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public object readExcel(int x,int y)
        {
            Excel.Range rangeData = (Excel.Range)eSheet.Cells[x, y];
            return rangeData.Value2;
        }
        /// <summary>
        /// 将一维double型数组写入到指定行
        /// </summary>
        /// <param name="x"></param>
        /// <param name="value"></param>
        public void writeArray(int x, double[] value)
        {
            for (int i = 1; i <= value.Length; i++)
            {
                Excel.Range rangeData = (Excel.Range)eSheet.Cells[x, i];
                rangeData.Value2 = value[i - 1];
            }
        }
        /// <summary>
        /// 将一维int型数组写入指定行
        /// </summary>
        /// <param name="x"></param>
        /// <param name="value"></param>
        public void writeArray(int x, int[] value)
        {
            for (int i = 1; i <= value.Length; i++)
            {
                Excel.Range rangeData = (Excel.Range)eSheet.Cells[x, i];
                rangeData.Value2 = value[i - 1];
            }
        }
        /// <summary>
        /// 将按指定分隔符分割的string型数写入到指定行
        /// </summary>
        /// <param name="x"></param>
        /// <param name="value"></param>
        public void writeString(int x, string value)
        {
            string[] temp = value.Split('\t');
            for (int i = 1; i <= temp.Length; i++)
            {
                Excel.Range rangeData = (Excel.Range)eSheet.Cells[x, i];
                rangeData.Value2 = temp[i - 1];
            }
        }
        /// <summary>
        /// 将一维的char型数组写入到指定行
        /// </summary>
        /// <param name="x"></param>
        /// <param name="value"></param>
        public void writeArray(int x, char[] value)
        {
            for (int i = 1; i <= value.Length; i++)
            {
                Excel.Range rangeData = (Excel.Range)eSheet.Cells[x, i];
                rangeData.Value2 = value[i - 1];
            }
        }
        /// <summary>
        /// 将一个double型二维数组写入到Excel
        /// </summary>
        /// <param name="value"></param>
        public void writeArrays(double[,] value)
        {
            for(int i=1;i<=value.GetLength(0);i++)
                for (int j = 1; j <= value.GetLength(1); j++)
                {
                    Excel.Range rangeData = (Excel.Range)eSheet.Cells[i, j];
                    rangeData.Value2 = value[i - 1, j - 1];
                }
        }
        /// <summary>
        /// 将一个int型二维数组写进Excel
        /// </summary>
        /// <param name="value"></param>
        public void writeArrays(int[,] value)
        {
            for (int i = 1; i <= value.GetLength(0); i++)
                for (int j = 1; j <= value.GetLength(1); j++)
                {
                    Excel.Range rangeData = (Excel.Range)eSheet.Cells[i, j];
                    rangeData.Value2 = value[i - 1, j - 1];
                }
        }
        /// <summary>
        /// 将以分隔符分割的文本文件转换成指定目录下的excel文件，
        /// 经测试，3600行数据转换耗时75秒左右，而采用边读边写方式时，耗时稍微增加
        /// </summary>
        /// <param name="txtPath"></param>
        /// <param name="directoryPath"></param>
        /// <summary>
        /// 将以分隔符分割的文本文件转换成指定目录下的excel文件，成功的话返回true，否则返回false
        /// 经测试，3600行数据转换耗时75秒左右，而采用边读边写方式时，耗时稍微增加
        /// </summary>
        /// <param name="txtPath"></param>
        /// <param name="directoryPath"></param>
        public bool txt2Xlsx(string txtPath,string xlspath)
        {
            try
            {
                StreamReader readData = new StreamReader(txtPath, Encoding.Default);//开启读的文件流
                string lineData = null;//每一行的数据，标准格式，以分隔符分割
                ArrayList dataList = new ArrayList();
                while ((lineData = readData.ReadLine()) != null)
                {
                    dataList.Add(lineData);
                }
                readData.Close();

                this.eApplication = new Excel.ApplicationClass();
                this.eWorkbook = this.eApplication.Workbooks.Add(true);
                this.eApplication.Visible = false;//创建xlsx的进程
                this.eSheet = (Excel.Worksheet)eWorkbook.Sheets[1];//默认对第一个sheet进行操作

                int line = 1;//写入的Excel的行数
                foreach (string str in dataList)
                {
                    this.writeString(line++, str);
                }
                char[] ch = { '\\', '.' };
                string[] temp = xlspath.Split(ch);

                this.eWorkbook.SaveAs(xlspath, nothing, nothing,
                   nothing, nothing, nothing, Excel.XlSaveAsAccessMode.xlShared,
                   nothing, nothing, nothing, nothing, nothing);//存储路径
                this.eApplication.Quit();
                
                File.Delete(txtPath);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
