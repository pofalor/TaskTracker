namespace TaskTracker.Core.DataAccess.src.BaseClasses.impl
{
    public abstract class PersistentEntity<TKey> : IPersistentEntity where TKey : IEquatable<TKey>
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public required TKey Id { get; set; }

        /// <summary>
        /// Дата создания
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Дата последнего изменения
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Версия объекта
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Поле означающее удалена сущность или нет
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <inheritdoc />
        object IPersistentEntity.Id
        {
            get { return Id; }
            set { Id = (TKey)value; }
        }
    }
}
