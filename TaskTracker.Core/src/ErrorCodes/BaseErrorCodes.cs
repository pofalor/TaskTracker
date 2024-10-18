using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskTracker.Core.src.Constants;
using TaskTracker.Core.src.Resources.ErrorCodes;

namespace TaskTracker.Core.src.ErrorCodes
{
    public enum BaseErrorCodes
    {
        /// <summary>
        /// Ошибка получения эл-ов
        /// </summary>
        [ErrorMessage(typeof(BaseErrorCodeResources), nameof(GetItemsError))]
        GetItemsError = SystemErrorCodes.InvalidRequest + ErrorConstants.EnumErrorCodeCount,

        /// <summary>
        /// Ошибка в модели
        /// </summary>
        [ErrorMessage(typeof(BaseErrorCodeResources), nameof(ModelInvalid))]
        ModelInvalid = GetItemsError + 1,

        /// <summary>
        /// Не удаётся удалить элемент
        /// </summary>
        [ErrorMessage(typeof(BaseErrorCodeResources), nameof(DeleteItemError))]
        DeleteItemError = ModelInvalid + 1,

        /// <summary>
        /// Ошибка в модели
        /// </summary>
        [ErrorMessage(typeof(BaseErrorCodeResources), nameof(CreateItemError))]
        CreateItemError = DeleteItemError + 1,

        /// <summary>
        /// Ошибка получения эл-а
        /// </summary>
        [ErrorMessage(typeof(BaseErrorCodeResources), nameof(GetItemError))]
        GetItemError = CreateItemError + 1,
    }
}
