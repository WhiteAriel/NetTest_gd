using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

namespace NetTest
{
    class UrlParser
    {
        [DllImport("UrlParser.dll")]
        public static extern void Video_Parse(string webUrl);

        public static string GetRealUrl(string url)
        {
            string urlFile = "RealUrl.txt";
            string realUrl="";
            if (File.Exists(urlFile))
            {
                File.Delete(urlFile);
            }

            //地址解析
            Video_Parse(url);

            if (File.Exists(urlFile))
            {
                StreamReader sr = new StreamReader(urlFile);
                realUrl = sr.ReadToEnd();
                sr.Close();
                File.Delete(urlFile);
            }
            else
            {
                realUrl = "failed to get real url";
            }

            return realUrl;
        }
    }
}
