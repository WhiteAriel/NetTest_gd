using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using IfacesEnumsStructsClasses;

namespace NetTest
{
    class CacheClear
    {
        private csExWB.cEXWB m_CurWB = null;

        public bool ClearHistory()
        {
            return m_CurWB.ClearHistory();
        }
        public int ClearAllCookies(string FromSite)
        {
            int ideleted = 0;
            System.Collections.ArrayList results = WinApis.FindUrlCacheEntries(
                AllForms.SetupCookieCachePattern(FromSite, AllForms.COOKIE));
            foreach (INTERNET_CACHE_ENTRY_INFO entry in results)
            {
                //Must have a SourceUrlName and LocalFileName
                if (
                    (!string.IsNullOrEmpty(entry.lpszSourceUrlName)) &&
                    (entry.lpszSourceUrlName.Trim().Length > 0) &&
                    (!string.IsNullOrEmpty(entry.lpszLocalFileName)) &&
                    (entry.lpszLocalFileName.Trim().Length > 0)
                    )
                {
                    WinApis.DeleteUrlCacheEntry(entry.lpszSourceUrlName);
                    ideleted++;
                }
            }
            return ideleted;
        }

        public int ClearAllCache(string FromSite)
        {
            int ideleted = 0;

            System.Collections.ArrayList results = WinApis.FindUrlCacheEntries(
                AllForms.SetupCookieCachePattern(FromSite, AllForms.VISITED));
            foreach (INTERNET_CACHE_ENTRY_INFO entry in results)
            {
                //Avoid Null or empty entries
                if ((!string.IsNullOrEmpty(entry.lpszSourceUrlName)) &&
                    (entry.lpszSourceUrlName.Trim().Length > 0))
                {
                    WinApis.DeleteUrlCacheEntry(entry.lpszSourceUrlName);
                    ideleted++;
                }
            }

            return ideleted;
        }
    }
}
