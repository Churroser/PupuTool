using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PupuTool
{
    /// <summary>
    /// 日志记录类
    /// </summary>
    public class LogHelper
    {
        private static bool isinit = false;

        static LogHelper()
        {
            if (isinit == false)
            {
                isinit = true;
                SetConfig();
            }
        }

        //private static readonly log4net.ILog LogInfo = log4net.LogManager.GetLogger("LogInfo");

        //private static readonly log4net.ILog LogError = log4net.LogManager.GetLogger("LogError");

        //private static readonly log4net.ILog LogException = log4net.LogManager.GetLogger("LogException");

        //private static readonly log4net.ILog LogComplement = log4net.LogManager.GetLogger("LogComplement");

        //private static readonly log4net.ILog LogDubug = log4net.LogManager.GetLogger("LogDubug");


        private static bool LogInfoEnable = false;
        private static bool LogErrorEnable = false;
        private static bool LogExceptionEnable = false;
        private static bool LogComplementEnable = false;
        private static bool LogDubugEnable = false;
        //private static bool LogFatalEnabled = false;

        private static Logger logger = LogManager.GetCurrentClassLogger();



        /// <summary>
        /// 设置初始值。
        /// </summary>
        public static void SetConfig()
        {
            //log4net.Config.DOMConfigurator.Configure();
            //LogInfoEnable=LogInfo.IsInfoEnabled;
            //LogErrorEnable=LogError.IsErrorEnabled;
            //LogExceptionEnable=LogException.IsErrorEnabled;
            //LogComplementEnable=LogComplement.IsErrorEnabled;
            //LogDubugEnable = LogDubug.IsDebugEnabled;

            LogInfoEnable = logger.IsInfoEnabled;
            LogErrorEnable = logger.IsErrorEnabled;
            LogExceptionEnable = logger.IsErrorEnabled;
            LogComplementEnable = logger.IsTraceEnabled;
            //LogFatalEnabled = logger.IsFatalEnabled;
            LogDubugEnable = logger.IsDebugEnabled;

        }
        /// <summary>
        /// 写入普通日志消息
        /// </summary>
        /// <param name="info"></param>
        public static void WriteInfo(string info)
        {
            if (LogInfoEnable)
            {
                logger.Info(BuildMessage(info));
                //LogInfo.Info(info);
            }
        }
        /// <summary>
        /// 写入Debug日志消息
        /// </summary>
        /// <param name="info"></param>
        public static void WriteDebug(string info)
        {
            if (LogDubugEnable)
            {
                logger.Debug(BuildMessage(info));
                //LogDubug.Debug(info);
            }
        }
        /// <summary>
        /// 写入错误日志消息
        /// </summary>
        /// <param name="info"></param>
        public static void WriteError(string info)
        {
            if (LogErrorEnable)
            {
                logger.Error(BuildMessage(info));
                //LogError.Error(info);
            }
        }

        /// <summary>
        /// 写入异常日志信息
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="frameIndex"></param>
        public static void WriteException(Exception ex, int frameIndex = 0)
        {
            string info = string.Empty;
            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(true);
            if (st.FrameCount > frameIndex)
            {
                info = st.GetFrame(frameIndex + 1).GetMethod().Name;
            }
            WriteException(info, ex);

        }

        /// <summary>
        /// 写入异常日志信息
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ex"></param>
        public static void WriteException(string info, Exception ex)
        {
            if (LogExceptionEnable)
            {
                logger.Error(BuildMessage(info, ex));
                //LogException.Error(info,ex);
            }
        }
        /// <summary>
        /// 写入严重错误日志消息
        /// </summary>
        /// <param name="info"></param>
        public static void WriteFatal(string info)
        {
            if (LogErrorEnable)
            {
                logger.Fatal(BuildMessage(info));
            }
        }
        /// <summary>
        /// 写入补充日志
        /// </summary>
        /// <param name="info"></param>
        public static void WriteComplement(string info)
        {
            if (LogComplementEnable)
            {
                logger.Trace(BuildMessage(info));
                //LogComplement.Error(info);
            }
        }
        /// <summary>
        /// 写入补充日志
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ex"></param>
        public static void WriteComplement(string info, Exception ex)
        {
            if (LogComplementEnable)
            {
                logger.Trace(BuildMessage(info, ex));
            }
        }


        static string BuildMessage(string info)
        {
            return BuildMessage(info, null);
        }

        static string BuildMessage(string info, Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Time:{0}-{1}\r\n", DateTime.Now, info);
            if (ex != null)
            {
                sb.AppendFormat("Exception:{0}\r\n", ex);
            }
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// 写入自定义日志到自定义目录,本方法对应的Nlog.config配置示例：
        ///  &lt;targets>
        ///    &lt;target name="LogCustom" xsi:type="File" layout="${message}"
        ///          fileName="${logDirectory}\${event-context:DirOrPrefix}${date:format=yyyyMMddHH}.txt">&lt;/target>
        ///  &lt;/targets>
        ///  &lt;rules>
        ///    &lt;logger name="LogCustom" level="Warn" writeTo="LogCustom" />
        /// </summary>
        /// <param name="message">要写入的消息</param>
        /// <param name="dirOrPrefix">
        /// 写入到的子目录或文件前缀，如果字符串包含\，则是子目录
        /// 比如 aa\bb 则写入的文件名为aa目录下的bb开头加日期
        /// </param>
        public static void WriteCustom(string message, string dirOrPrefix)
        {
            WriteCustom(message, dirOrPrefix, null, true);
        }

        /// <summary>
        /// 写入自定义日志到自定义目录,本方法对应的Nlog.config配置示例：
        ///  &lt;targets>
        ///    &lt;target name="LogCustom" xsi:type="File" layout="${message}"
        ///          fileName="${logDirectory}\${event-context:DirOrPrefix}${date:format=yyyyMMddHH}.txt">&lt;/target>
        ///  &lt;/targets>
        ///  &lt;rules>
        ///    &lt;logger name="LogCustom" level="Warn" writeTo="LogCustom" />
        /// </summary>
        /// <param name="message">要写入的消息</param>
        /// <param name="dirOrPrefix">
        /// 写入到的子目录或文件前缀，如果字符串包含\，则是子目录
        /// 比如 aa\bb 则写入的文件名为aa目录下的bb开头加日期
        /// </param>
        /// <param name="addIpUrl">是否要附加ip和url等信息</param>
        public static void WriteCustom(string message, string dirOrPrefix, bool addIpUrl)
        {
            WriteCustom(message, dirOrPrefix, null, addIpUrl);
        }


        /// <summary>
        /// 写入自定义日志到自定义目录,本方法对应的Nlog.config配置示例：
        ///  &lt;targets>
        ///    &lt;target name="LogCustom" xsi:type="File" layout="${message}"
        ///          fileName="${logDirectory}\${event-context:DirOrPrefix}${date:format=yyyyMMddHH}${event-context:Suffix}.txt">&lt;/target>
        ///  &lt;/targets>
        ///  &lt;rules>
        ///    &lt;logger name="LogCustom" level="Warn" writeTo="LogCustom" />
        /// </summary>
        /// <param name="message">要写入的消息</param>
        /// <param name="dirOrPrefix">
        /// 写入到的子目录或文件前缀，如果字符串包含\，则是子目录
        /// 比如 aa\bb 则写入的文件名为aa目录下的bb开头加日期
        /// </param>
        /// <param name="suffix">写入到的文件后缀</param>
        public static void WriteCustom(string message, string dirOrPrefix, string suffix)
        {
            WriteCustom(message, dirOrPrefix, suffix, true);
        }

        /// <summary>
        /// 写入自定义日志到自定义目录,本方法对应的Nlog.config配置示例：
        ///  &lt;targets>
        ///    &lt;target name="LogCustom" xsi:type="File" layout="${message}"
        ///          fileName="${logDirectory}\${event-context:DirOrPrefix}${date:format=yyyyMMddHH}${event-context:Suffix}.txt">&lt;/target>
        ///  &lt;/targets>
        ///  &lt;rules>
        ///    &lt;logger name="LogCustom" level="Warn" writeTo="LogCustom" />
        /// </summary>
        /// <param name="message">要写入的消息</param>
        /// <param name="dirOrPrefix">
        /// 写入到的子目录或文件前缀，如果字符串包含\，则是子目录
        /// 比如 aa\bb 则写入的文件名为aa目录下的bb开头加日期
        /// </param>
        /// <param name="suffix">写入到的文件后缀</param>
        /// <param name="addIpUrl">是否要附加ip和url等信息</param>
        public static void WriteCustom(string message, string dirOrPrefix, string suffix, bool addIpUrl)
        {
            if (addIpUrl)
                message = BuildMessage(message);
            Logger logger1 = LogManager.GetLogger("LogCustom");
            LogEventInfo logEvent = new LogEventInfo(LogLevel.Warn, logger1.Name, message);
            logEvent.Properties["DirOrPrefix"] = dirOrPrefix;
            if (suffix != null)
                logEvent.Properties["Suffix"] = suffix;
            logger1.Log(logEvent);
        }
    }
}
