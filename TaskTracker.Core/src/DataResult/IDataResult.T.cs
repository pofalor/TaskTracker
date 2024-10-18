namespace TaskTracker.Core.src.DataResult
{
    public interface IDataResult<T> : IDataResult
    {
        T Data { get; set; }
    }
}
