using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Enums;

namespace TaskTracker.Core.src.Models.ResponseModels
{
    public class WorkSpaceModel
    {
        public int Id { get; set; }

        /// <summary>
        /// Название рабочего пространства
        /// </summary>
        public string Name { get; set; } = null!;
        public WorkSpaceType WorkSpaceType { get; set; }

        /// <summary>
        /// Ссылка на управляющего компании
        /// </summary>
        public int DirectorUserId { get; set; }

        //Все поля ниже заполняются, если WorkSpaceType - Company

        /// <summary>
        /// Страна, в которой компания зарегистрирована
        /// </summary>
        public int? Country { get; set; }

        /// <summary>
        /// Дата регистрации в UTC
        /// </summary>
        public DateTime? RegistrationDate { get; set; }

        /// <summary>
        /// Юр. адрес
        /// </summary>
        public string? Address { get; set; }

        public int? INN { get; set; }
    }
}
