using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logger
{
    public class LoggingInformationData
    {
        public string Text { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }
}
