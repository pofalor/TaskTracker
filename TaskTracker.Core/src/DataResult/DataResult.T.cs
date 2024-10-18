using TaskTracker.Utils.src.Extensions;

namespace TaskTracker.Core.src.DataResult
{
    public class DataResult<T> : IDataResult<T>
    {
        public DataResult()
        {
            Errors = [];
        }

        public DataResult(T data) : this()
        {
            Data = data;
        }

        public T Data { get; set; } = default!;

        public IList<IDataError> Errors { get; private set; }

        public bool Success
        {
            get { return !Errors.Any(); }
        }
    }
}