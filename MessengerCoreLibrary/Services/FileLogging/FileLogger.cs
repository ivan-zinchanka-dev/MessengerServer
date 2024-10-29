using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace MessengerCoreLibrary.Services.FileLogging
{
    public class FileLogger : ILogger, IDisposable
    {
        private readonly string _fullFileName;
        private readonly string _categoryName;
        private readonly object _threadLock;
    
        public FileLogger(string fullFileName, string categoryName = null)
        {
            _fullFileName = fullFileName;
            _categoryName = categoryName;
            _threadLock = new object();
        }
    
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, 
            Func<TState, Exception, string> formatter)
        {
            lock (_threadLock)
            {
                string logContent = _categoryName != null
                    ? $"[{DateTime.Now}] <{_categoryName}> {nameof(logLevel)}: {formatter(state, exception)}\n"
                    : $"[{DateTime.Now}] {nameof(logLevel)}: {formatter(state, exception)}\n";
            
                if (exception != null)
                {
                    logContent += exception.StackTrace;
                }
            
                File.AppendAllText(_fullFileName, logContent);
            } 
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }

        public void Dispose() { }
    }
}