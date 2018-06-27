using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace WcfLogger
{
    public class LoggingOutputsData
    {
        public ParameterInfo[] OutputParameters { get; set; }
        public object[] OutputValues { get; set; }

        public override string ToString()
        {
            if (OutputParameters == null && OutputValues == null)
            {
                return string.Empty;
            }

            if (OutputParameters == null)
            {
                return ObjectToStringConverter.ConvertCollectionToString(OutputValues);
            }

            if (OutputValues == null)
            {
                return ObjectToStringConverter.ConvertCollectionToString(OutputParameters.Select(p => p.Name));
            }

            return ObjectToStringConverter.ConvertSomeToString(OutputParameters.Select(p => p.Name).ToArray(), OutputValues);
        }

    }
}
