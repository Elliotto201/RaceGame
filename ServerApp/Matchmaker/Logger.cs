using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matchmaking
{
    internal sealed class Logger
    {
        public event Action<Log> OnLog;

        public void Log(string message, LogType logType)
        {
            var log = new Log
            {
                LogMessage = message,
                Type = logType,
            };

            OnLog?.Invoke(log);
        }
    }

    public struct Log
    {
        public string LogMessage;
        public LogType Type;
    }

    public enum LogType : byte
    {
        Message = 0,
        Warning = 2,
        Error = 4,
    }
}
