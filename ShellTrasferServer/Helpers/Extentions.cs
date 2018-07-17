using PostSharp.Patterns.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellTrasferServer
{
    static class Extentions
    {
        [Log(AttributeExclude = true)]
        public static Queue<T> DeleteAt<T>(this Queue<T> queue,int index)
        {
            var listQueue = queue.ToList();
            listQueue.RemoveAt(index);
            return new Queue<T>(listQueue);
        }
    }
}
