namespace ShellTrasferServer.Data
{
    public interface IActiveClientLocks
    {
       void PulseAll();

        void Add(object lockObj);

        void Remove(object lockObj);
    }
}
