using PostSharp.Patterns.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WcfLogger
{
    class PostSharpLoggingStrategy : ILoggingStrategy
    {
        public bool Log(LoggingArgument arg)
        {
            var logger = Logger.GetLogger();
            if (logger == null)
                throw new Exception("The PostSharp logger returned null!");

            logger.Write(LogLevel.Trace, arg.ToString());
            return true;
        }
    }
}
