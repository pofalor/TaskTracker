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
        /// Не удаётся создать или изменить сущность(вокрспейс или инвайт)
        /// </summary>
        [ErrorMessage(typeof(WorkSpaceErrorCodeResources), nameof(CannotCreateOrEditWorkspace))]
        CannotCreateOrEditWorkspace = CanCreateOnlyOnePersonalWorkspace + 1,

        /// <summary>
        /// Не удаётся создать или изменить воркспейс, т.к. нет прав у пользователя
        /// </summary>
        [ErrorMessage(typeof(WorkSpaceErrorCodeResources), nameof(AccessDenied))]
        AccessDenied = CannotCreateOrEditWorkspace + 1,


        /// <summary>
        /// Не удаётся получить данные о приглашениях юзера
        /// </summary>
        [ErrorMessage(typeof(WorkSpaceErrorCodeResources), nameof(CannotGetWpsRequests))]
        CannotGetWpsRequests = AccessDenied + 1,

        /// <summary>
        /// Не удаётся создать инвайт, т.к. не задана ссылка на рабочее пространство
        /// </summary>
        [ErrorMessage(typeof(WorkSpaceErrorCodeResources), nameof(WorkspaceNotSet))]
        WorkspaceNotSet = CannotGetWpsRequests + 1,

        /// <summary>
        /// Не удаётся создать инвайт, т.к. не задана ссылка на юзера
        /// </summary>
        [ErrorMessage(typeof(WorkSpaceErrorCodeResources), nameof(WpsInviteUserIdNotSet))]
        WpsInviteUserIdNotSet = WorkspaceNotSet + 1,

        /// <summary>
        /// Не удаётся создать инвайт, т.к. не задана ссылка на инвайтера
        /// </summary>
        [ErrorMessage(typeof(WorkSpaceErrorCodeResources), nameof(WpsInviterIdNotSet))]
        WpsInviterIdNotSet = WpsInviteUserIdNotSet + 1,

        /// <summary>
        /// Не удаётся создать инвайт, т.к. не задана дата запроса
        /// </summary>
        [ErrorMessage(typeof(WorkSpaceErrorCodeResources), nameof(WpsInviteReqDateNotSet))]
        WpsInviteReqDateNotSet = WpsInviterIdNotSet + 1,

        /// <summary>
        /// Не удаётся создать инвайт, т.к. дата запроса в будущем
        /// </summary>
        [ErrorMessage(typeof(WorkSpaceErrorCodeResources), nameof(WpsInviteReqDateFuture))]
        WpsInviteReqDateFuture = WpsInviteReqDateNotSet + 1,

        /// <summary>
        /// Не удаётся создать инвайт, т.к. такого воркспейса не существует
        /// </summary>
        [ErrorMessage(typeof(WorkSpaceErrorCodeResources), nameof(WpsForInviteNotExists))]
        WpsForInviteNotExists = WpsInviteReqDateFuture + 1,

        /// <summary>
        /// Не удаётся создать инвайт, т.к. инвайт уже создан
        /// </summary>
        [ErrorMessage(typeof(WorkSpaceErrorCodeResources), nameof(ActiveInviteAlreadyExists))]
        ActiveInviteAlreadyExists = WpsForInviteNotExists + 1,

        /// <summary>
        /// Не удаётся создать или изменить инвайт
        /// </summary>
        [ErrorMessage(typeof(WorkSpaceErrorCodeResources), nameof(CannotCreateOrEditInviteWsp))]
        CannotCreateOrEditInviteWsp = ActiveInviteAlreadyExists + 1,

        /// <summary>
        /// Юзер уже член рабочего пространства
        /// </summary>
        [ErrorMessage(typeof(WorkSpaceErrorCodeResources), nameof(UserAlreadyInWsp))]
        UserAlreadyInWsp = CannotCreateOrEditInviteWsp + 1,
    }
}
