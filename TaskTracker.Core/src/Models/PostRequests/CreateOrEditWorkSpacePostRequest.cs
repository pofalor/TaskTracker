using System.Globalization;
using TaskTracker.Core.src.Constants;
using TaskTracker.Core.src.Enums;

namespace TaskTracker.Core.src.Models.PostRequests
{
    public class CreateOrEditWorkspacePostRequest : BasePostRequest
    {
        public int Id { get; set; }
        /// <summary>
        /// Название рабочего пространства
        /// </summary>
        public string Name { get; set; } = null!;
        public WorkspaceType WorkspaceType { get; set; }

        /// <summary>
        /// Ссылка на управляющего компании
        /// </summary>
        public int? DirectorUserId { get; set; }

        //Все поля ниже заполняются, если WorkspaceType - Company

        /// <summary>
        /// Страна, в которой компания зарегистрирована
        /// </summary>
        public int? Country { get; set; }

        /// <summary>
        /// Дата регистрации в UTC
        /// </summary>
        public string? RegistrationDate { get; set; }

        /// <summary>
        /// Юр. адрес
        /// </summary>
        public string? Address { get; set; }

        public string? INN { get; set; }

        public DateTime? RegistrationDateTime
        { 
            get
            {
                return !string.IsNullOrEmpty(RegistrationDate) ? DateTime.ParseExact(RegistrationDate, DateFormatConstants.FrontInputFormat, CultureInfo.InvariantCulture) : null;
            } 
        }

        public WorkspaceReviewStatus? ReviewStatus { get; set; }
    }
}
