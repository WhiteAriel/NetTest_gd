using log4net;
using System;
using System.IO;

namespace NetLog
{
    public class Log
    {
        private const string SError = "NetError";
        private const string SWarn = "NetWarn";
        private const string SInfo = "NetInfo";
        private const string SConsole = "NetConsole";

        static Log()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory + @"\log4net.config";
            log4net.Config.XmlConfigurator.Configure(new FileInfo(path));
        }

        public static log4net.ILog GetLog(string logName)
        {
            var log = log4net.LogManager.GetLogger(logName);
            return log;
        }

        //public static void Debug(string message)
        //{
        //    var log = log4net.LogManager.GetLogger(SDebug);
        //    if (log.IsDebugEnabled)
        //        log.Debug(message);
        //}

        //public static void Debug(string message, Exception ex)
        //{
        //    var log = log4net.LogManager.GetLogger(SDebug);
        //    if (log.IsDebugEnabled)
        //        log.Debug(message, ex);
        //}
		
		public static void Warn(string message)
        {
            var log = log4net.LogManager.GetLogger(SWarn);
            if (log.IsWarnEnabled)
                log.Warn(message);
        }

		public static void Warn(string message, Exception ex)
        {
            var log = log4net.LogManager.GetLogger(SWarn);
            if (log.IsWarnEnabled)
                log.Warn(message, ex);
        }

        public static void Error(string message)
        {
            var log = log4net.LogManager.GetLogger(SError);
            if (log.IsErrorEnabled)
                log.Error(message);
        }

        public static void Error(string message, Exception ex)
        {
            var log = log4net.LogManager.GetLogger(SError);
            if (log.IsErrorEnabled)
                log.Error(message, ex);
        }

        //public static void Fatal(string message)
        //{
        //   var log = log4net.LogManager.GetLogger(SError);
        //    if (log.IsFatalEnabled)
        //        log.Fatal(message);
        //}
		
        //public static void Fatal(string message, Exception ex)
        //{
        //    var log = log4net.LogManager.GetLogger(SError);
        //    if (log.IsFatalEnabled)
        //        log.Fatal(message, ex);
        //}

        public static void Info(string message)
        {
            log4net.ILog log = log4net.LogManager.GetLogger(SInfo);
            if (log.IsInfoEnabled)
                log.Info(message);
        }

        public static void Console(string message)
        {
            log4net.ILog log = log4net.LogManager.GetLogger(SConsole);
            if (log.IsDebugEnabled)
                log.Debug(message);
        }

        public static void Console(string message, Exception ex)
        {
            var log = log4net.LogManager.GetLogger(SConsole);
            if (log.IsDebugEnabled)
                log.Debug(message, ex);
        }

    }
}