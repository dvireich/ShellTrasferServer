using PostSharp.Patterns.Diagnostics;
using ShellTrasferServer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
    [Log(AttributeExclude = true)]
    public class UserClientManager
    {
        private ConcurrentDictionary<string, ICallBack> _statusCallBacks = new ConcurrentDictionary<string, ICallBack>();
        public ConcurrentDictionary<string, ICallBack> StatusCallBacks { get { return _statusCallBacks; } }
        private ConcurrentDictionary<string, ICallBack> _callBacks = new ConcurrentDictionary<string, ICallBack>();
        public ConcurrentDictionary<string, ICallBack> CallBacks { get { return _callBacks; } }
        private Dictionary<string, string> _nickNames = new Dictionary<string, string>();
        public Dictionary<string, string> NickNames { get { return _nickNames; } }
        private HashSet<string> _deleted = new HashSet<string>();
        private HashSet<string> _deletedTasks = new HashSet<string>();
        public HashSet<string> Deleted { get { return _deleted; } }
        public HashSet<string> DeletedTasks { get { return _deletedTasks; } }


        private string _selectedClient;
        public string SelectedClient
        {
            get
            {
                return _selectedClient;
            }
            set
            {
                _selectedClient = value;
            }
        }
    }
}
