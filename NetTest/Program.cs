using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Management;

namespace NetTest
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            //zhi ding mac,fang zhi kao bei
            //string mac = GetMACAddress().Replace(":","-");
            //if (mac.ToUpper()!="50-E5-49-A8-0D-76")
            //{
            //    //MessageBox.Show("");
            //    return;
            //}
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain());
        }

        static string GetMACAddress()
        {
            string MoAddress = "";
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                if ((bool)mo["IPEnabled"] == true)
                    MoAddress = mo["MacAddress"].ToString();
                mo.Dispose();
            }
            return MoAddress;
        }
    }
    
}