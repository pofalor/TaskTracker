using TaskTracker.Core.src.Constants;
using TaskTracker.Core.src.Resources.ErrorCodes;

namespace TaskTracker.Core.src.ErrorCodes
{
    public enum WorkSpaceErrorCodes
    {
        /// <summary>
        /// Не удалось получить информацию о рабочих пространствах
        /// </summary>
        [ErrorMessage(typeof(WorkSpaceErrorCodeResources), nameof(CannotGetMyWorkspaces))]
        CannotGetMyWorkspaces = UserErrorCodes.CannotGetUser + ErrorConstants.EnumErrorCodeCount,

        /// <summary>
        /// Значение страны не задано
        /// </summary>
        [ErrorMessage(typeof(WorkSpaceErrorCodeResources), nameof(CountryNull))]
        CountryNull = CannotGetMyWorkspaces + 1,

        /// <summary>
        /// Значение инн не задано
        /// </summary>
        [ErrorMessage(typeof(WorkSpaceErrorCodeResources), nameof(INNNull))]
        INNNull = CountryNull + 1,

        /// <summary>
        /// Значение адреса не задано
        /// </summary>
        [ErrorMessage(typeof(WorkSpaceErrorCodeResources), nameof(AddressNull))]
        AddressNull = INNNull + 1,

        /// <summary>
        /// Значение даты регистрации не задано
        /// </summary>
        [ErrorMessage(typeof(WorkSpaceErrorCodeResources), nameof(RegistrationDateNull))]
        RegistrationDateNull = AddressNull + 1,

        /// <summary>
        /// Компания с таким инн уже существует
        /// </summary>
        [ErrorMessage(typeof(WorkSpaceErrorCodeResources), nameof(CompanyWithInnAlreadyExists))]
        CompanyWithInnAlreadyExists = RegistrationDateNull + 1,

        /// <summary>
        /// Компания с теми же данными существует
        /// </summary>
        [ErrorMessage(typeof(WorkSpaceErrorCodeResources), nameof(CompanyWithDataAlreadyExists))]
        CompanyWithDataAlreadyExists = CompanyWithInnAlreadyExists + 1,

        /// <summary>
        /// Компания с тем же именем существует
        /// </summary>
        [ErrorMessage(typeof(WorkSpaceErrorCodeResources), nameof(CompanyWithNameAlreadyExists))]
        CompanyWithNameAlreadyExists = CompanyWithDataAlreadyExists + 1,

        /// <summary>
        /// Можно создать только один личный воркспейс
        /// </summary>
        [ErrorMessage(typeof(WorkSpaceErrorCodeResources), nameof(CanCreateOnlyOnePersonalWorkspace))]
        CanCreateOnlyOnePersonalWorkspace = CompanyWithNameAlreadyExists + 1,

        /// <summary>
        /// Не удаётся создать или изменить воркспейс
        /// </summary>
        [ErrorMessage(typeof(WorkSpaceErrorCodeResources), nameof(CannotCreateOrEditWorkspace))]
        CannotCreateOrEditWorkspace = CanCreateOnlyOnePersonalWorkspace + 1,
    }
}
