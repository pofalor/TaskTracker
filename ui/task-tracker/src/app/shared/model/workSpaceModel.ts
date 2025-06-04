import { UserTeamRole } from "../enums/user-team-role";
import { WorkspaceType } from "../enums/work-space-type";
import { WorkspaceReviewStatus } from "../enums/workspace-review-status";

export class WorkspaceModel{

    public id! : number;
    
    /**  Название рабочего пространства */
    public name! : string;
    
    public workspaceType! : WorkspaceType;
    
    /** Ссылка на управляющего компании */
    public directorUserId! : number;

    public teamRole!: UserTeamRole;
    
    //Все поля ниже заполняются, если WorkspaceType - Company
    
    /** Страна, в которой компания зарегистрирована */
    public country : number | undefined;
    
    /** Дата регистрации в UTC */
    public registrationDate : string | undefined;
    
    /** Юр. адрес */
    public address : string | undefined;
    
    public inn : string | undefined;

    public reviewStatus: WorkspaceReviewStatus | undefined;
}