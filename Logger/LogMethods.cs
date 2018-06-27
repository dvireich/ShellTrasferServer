using System;
using System.Diagnostics;
using Logger;

namespace Logger
{
    public class LogMethods
    {
        public static void StaticMethodStarted()
        {
            StackTrace stackTrace = new StackTrace();
            var callerMethodName = stackTrace.GetFrame(1).GetMethod().Name;
            Console.WriteLine($"Before Method: {callerMethodName}");
        }

        public static void StaticMethodCompleted()
        {
            StackTrace stackTrace = new StackTrace();
            var callerMethodName = stackTrace.GetFrame(1).GetMethod().Name;
            Console.WriteLine($"After Method {callerMethodName}");
        }

        public void MethodStarted()
        {
            Console.WriteLine("MethodStarted()");
        }

        public void MethodCompleted()
        {
            Console.WriteLine("MethodCompleted()");
        }
    }
}
