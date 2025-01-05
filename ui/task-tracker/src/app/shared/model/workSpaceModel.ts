import { UserTeamRole } from "../enums/user-team-role";
import { WorkSpaceType } from "../enums/work-space-type";

export class WorkSpaceModel{

    public id! : number;
    
    /**  Название рабочего пространства */
    public name! : string;
    
    public workSpaceType! : WorkSpaceType;
    
    /** Ссылка на управляющего компании */
    public directorUserId! : number;

    public teamRole!: UserTeamRole;
    
    //Все поля ниже заполняются, если WorkSpaceType - Company
    
    /** Страна, в которой компания зарегистрирована */
    public country : number | undefined;
    
    /** Дата регистрации в UTC */
    public registrationDate : string | undefined;
    
    /** Юр. адрес */
    public address : string | undefined;
    
    public iNN : number | undefined;
}