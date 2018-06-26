using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Logger
{
    public class LoggingContextDetails
    {
        public MethodInfo MethodDetails { get; set; }
        public object[] Inputs { get; set; }

        public bool LogErrors { get; set; }
        public bool LogWarnings { get; set; }
        public bool LogInformation { get; set; }

        #region LoggingStrategy
        private ILoggingStrategy _loggingStrategy;
        public ILoggingStrategy LoggingStrategy
        {
            get { return _loggingStrategy ?? (_loggingStrategy = new ConsoleLoggingStrategy()); }
            set { _loggingStrategy = value; }
        }
        #endregion
    }
}
