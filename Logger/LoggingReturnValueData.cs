using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logger
{
    public class LoggingReturnValueData
    {
        public object Value { get; set; }

        public override string ToString()
        {
            return ObjectToStringConverter.ConvertToString(Value);
        }
    }
}
