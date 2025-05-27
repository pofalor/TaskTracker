import { WorkspaceType } from "../../enums/work-space-type";
import { WorkspaceReviewStatus } from "../../enums/workspace-review-status";

export class CreateOrEditWorkspacePostRequest{

    public id : number | undefined;
    
    /**  Название рабочего пространства */
    public name! : string;
    
    public workspaceType! : WorkspaceType;
    
    /** Ссылка на управляющего компании */
    public directorUserId : number | undefined;
    
    //Все поля ниже заполняются, если WorkspaceType - Company
    
    /** Страна, в которой компания зарегистрирована */
    public country : number | undefined;
    
    /** Дата регистрации в UTC */
    public registrationDate : string | undefined;
    
    /** Юр. адрес */
    public address : string | undefined;
    
    public iNN : number | undefined;

    public reviewStatus : WorkspaceReviewStatus | undefined;
}
