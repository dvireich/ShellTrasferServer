using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Configuration;
using System.Configuration;
using System.Reflection;

namespace Logger
{
    public class LoggingBehaviorExtensionElement : BehaviorExtensionElement
    {
        public override Type BehaviorType
        {
            get { return typeof(LoggingBehaviorAttribute); }
        }

        protected override object CreateBehavior()
        {
            return new LoggingBehaviorAttribute
            {
                LogBeforeCall = LogBeforeCall,
                LogAfterCall = LogAfterCall,
                LogErrors = LogErrors,
                LogWarnings = LogWarnings,
                LogInformation = LogInformation,
                LoggingStrategyType = ConvertStringToType(LoggingStrategyType)
            };
        }

        private Type ConvertStringToType(string strType)
        {
            if (string.IsNullOrEmpty(strType))
            {
                return null;
            }

            Type res = null;

            try
            {
                int firstCommaIndex = strType.IndexOf(",");
                if (firstCommaIndex > 0)
                {
                    string typeFullName = strType.Substring(0, firstCommaIndex);
                    string assemblyFullName = strType.Substring(firstCommaIndex + 1);

                    Assembly typeAssembly = Assembly.Load(assemblyFullName);
                    if (typeAssembly != null)
                    {
                        res = typeAssembly.GetType(typeFullName);
                    }
                }
            }
            catch
            {
            }

            return res;
        }

        #region Properties

        [ConfigurationProperty("logBeforeCall", DefaultValue = true)]
        public bool LogBeforeCall
        {
            get { return (bool)this["logBeforeCall"]; }
            set { this["logBeforeCall"] = value; }
        }

        [ConfigurationProperty("logAfterCall", DefaultValue = true)]
        public bool LogAfterCall
        {
            get { return (bool)this["logAfterCall"]; }
            set { this["logAfterCall"] = value; }
        }

        [ConfigurationProperty("logErrors", DefaultValue = true)]
        public bool LogErrors
        {
            get { return (bool)this["logErrors"]; }
            set { this["logErrors"] = value; }
        }

        [ConfigurationProperty("logWarnings", DefaultValue = true)]
        public bool LogWarnings
        {
            get { return (bool)this["logWarnings"]; }
            set { this["logWarnings"] = value; }
        }

        [ConfigurationProperty("logInformation", DefaultValue = true)]
        public bool LogInformation
        {
            get { return (bool)this["logInformation"]; }
            set { this["logInformation"] = value; }
        }

        [ConfigurationProperty("loggingStrategyType")]
        public string LoggingStrategyType
        {
            get { return (string)this["loggingStrategyType"]; }
            set { this["loggingStrategyType"] = value; }
        }

        #endregion
    }
}
