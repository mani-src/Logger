using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Logger
{
    public class Logger
    {
        private ConcurrentQueue<LoggerItems> logQueue;
        private string logPath = string.Empty;
        private Thread loggerThread = null;
        private bool stopLogging = false;
        private int logLevel = 1;
        private delegate void QueueUpdatedHandler();
        private event QueueUpdatedHandler queueUpdated;
        private Mutex __lock = new Mutex(true); 

        private void SetLogLevel(string level)
        {
            switch (level.ToUpper())
            {
                case "DEBUG":
                    logLevel = 0;
                    break;
                case "INFO":
                    logLevel = 1;
                    break;
                case "WARNING":
                    logLevel = 2;
                    break;
                case "ERROR":
                    logLevel = 3;
                    break;
                case "EXCEPTION":
                    logLevel = 4;
                    break;
            }
        }

        private struct LoggerItems
        {
            public readonly string fileName;
            public readonly string memberName;
            public readonly string mode;
            public readonly string timeStamp;
            public readonly string message;

            public LoggerItems(string fileName, string memberName, string mode, string message)
            {
                timeStamp = DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss:fff tt");
                this.fileName = fileName;
                this.memberName = memberName;
                this.mode = mode;
                this.message = message;
            }
        }

        private void WriteFile()
        {
            try
            {
                LoggerItems item;
                while (true)
                {
                    if (logQueue.IsEmpty && stopLogging)
                    {
                        break;
                    }
                    else if (logQueue.IsEmpty && !stopLogging)
                    {
                        continue;
                    }
                    if (logQueue.TryDequeue(out item))
                    {
                        if (item.mode != "EXCEPTION")
                        {
                            File.AppendAllText(logPath, Environment.NewLine + "[" + item.timeStamp + "] [" + item.fileName + "] [" + item.memberName + "] [" + item.mode + "] : " + item.message);
                        }
                        else
                        {
                            string exceptionDecorator = string.Empty;
                            exceptionDecorator += "=".Multiply(101) + Environment.NewLine;
                            exceptionDecorator += "*".Multiply(46) + "EXCEPTION" + "*".Multiply(46) + Environment.NewLine;
                            exceptionDecorator += "=".Multiply(101) + Environment.NewLine;
                            File.AppendAllText(logPath, Environment.NewLine + "[" + item.timeStamp + "] [" + item.fileName + "] [" + item.memberName + "] [" + item.mode + "] : "
                                + Environment.NewLine + exceptionDecorator + item.message + Environment.NewLine + exceptionDecorator.Replace("EXCEPTION", "*".Multiply(9)));
                        }
                    }
                    Thread.Sleep(100);
                }
            }
            catch (Exception)
            {

            }
        }

        private void WriteLog()
        {
            try
            {
                __lock.WaitOne();
                while (!logQueue.IsEmpty)
                {
                    LoggerItems item;
                    if (logQueue.TryDequeue(out item))
                    {
                        if (item.mode != "EXCEPTION")
                        {
                            File.AppendAllText(logPath, Environment.NewLine + "[" + item.timeStamp + "] [" + item.fileName + "] [" + item.memberName + "] [" + item.mode + "] : " + item.message);
                        }
                        else
                        {
                            string exceptionDecorator = string.Empty;
                            exceptionDecorator += "=".Multiply(101) + Environment.NewLine;
                            exceptionDecorator += "*".Multiply(46) + "EXCEPTION" + "*".Multiply(46) + Environment.NewLine;
                            exceptionDecorator += "=".Multiply(101) + Environment.NewLine;
                            File.AppendAllText(logPath, Environment.NewLine + "[" + item.timeStamp + "] [" + item.fileName + "] [" + item.memberName + "] [" + item.mode + "] : "
                                + Environment.NewLine + exceptionDecorator + item.message + Environment.NewLine + exceptionDecorator.Replace("EXCEPTION", "*".Multiply(9)));
                        }
                    }
                }
            }
            finally
            {
                __lock.ReleaseMutex();
            }
        }

        /// <summary>
        /// Logger Class Constructor
        /// </summary>
        /// <param name="logLevel">Used to set the logger level to the Logger object, valid options: DEBUG, INFO, WARNING, ERROR, EXCEPTION</param>
        /// <param name="logDir">The path to the directory where the application wants to create the log files</param>
        /// <param name="appName">Name of the application</param>
        /// <param name="filePath">Path to the current file</param>
        public Logger(string logLevel = "INFO", string logDir = @"C:\", string appName = "", [CallerFilePath] string filePath = "")
        {
            try
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                if (string.IsNullOrEmpty(appName) || string.IsNullOrWhiteSpace(appName))
                {
                    logPath = logDir + fileName + ".log";
                }
                else
                {
                    logPath = logDir + appName + ".log";
                }
                logQueue = new ConcurrentQueue<LoggerItems>();
                SetLogLevel(logLevel);
                //loggerThread = new Thread(WriteFile);
                //loggerThread.Start();
                //queueUpdated += WriteLog;
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Method to start logging, must be called to enable logging
        /// </summary>
        public void Start()
        {
            loggerThread = new Thread(WriteFile);
            loggerThread.Start();
        }

        /// <summary>
        /// Method to process the current queue items and stop the logging, must be called before closing the application
        /// </summary>
        public void Stop()
        {
            stopLogging = true;
            loggerThread.Join();
        }

        public void Debug(string message, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
        {
            if (logLevel == 0)
            {
                logQueue.Enqueue(new LoggerItems(Path.GetFileNameWithoutExtension(filePath), memberName, "DEBUG", message));
            }
        }

        public void Info(string message, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
        {
            if (logLevel >= 0 && logLevel < 2)
            {
                logQueue.Enqueue(new LoggerItems(Path.GetFileNameWithoutExtension(filePath), memberName, "INFO", message));
            }
        }

        public void Warning(string message, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
        {
            if (logLevel >= 0 && logLevel < 3)
            {
                logQueue.Enqueue(new LoggerItems(Path.GetFileNameWithoutExtension(filePath), memberName, "WARNING", message));
            }
        }

        public void Error(string message, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
        {
            if (logLevel >= 0 && logLevel < 4)
            {
                logQueue.Enqueue(new LoggerItems(Path.GetFileNameWithoutExtension(filePath), memberName, "ERROR", message));
            }
        }

        public void Exception(Exception exp, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (logLevel >= 0 && logLevel <= 4)
            {
                logQueue.Enqueue(new LoggerItems(Path.GetFileNameWithoutExtension(filePath), memberName, "EXCEPTION", exp.ToString()));
            }
        }
    }
}
