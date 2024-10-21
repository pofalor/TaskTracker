using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Core.src.DataAccess.BaseClasses
{
    public abstract class PersistentEntity
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Дата создания
        /// </summary>
        public DateTime ObjectCreateDate { get; set; }

        /// <summary>
        /// Дата последнего изменения
        /// </summary>
        public DateTime ObjectEditDate { get; set; }

        /// <summary>
        /// Версия объекта
        /// </summary>
        public byte[]? Version { get; set; } = null!;

        /// <summary>
        /// Поле означающее удалена сущность или нет
        /// </summary>
        public bool IsDeleted { get; set; }
    }
}
