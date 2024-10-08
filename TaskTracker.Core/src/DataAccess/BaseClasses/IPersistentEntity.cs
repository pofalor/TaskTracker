namespace TaskTracker.Core.DataAccess.src.BaseClasses
{
    public interface IPersistentEntity
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        object Id { get; set; }

        /// <summary>
        /// Дата создания
        /// </summary>
        DateTime CreatedAt { get; set; }

        /// <summary>
        /// Дата последнего изменения
        /// </summary>
        DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Поле означающее удалена сущность или нет
        /// </summary>
        bool IsDeleted { get; set; }
    }
}
