export class UserModel{
    id!: number;
    
    name!: string;

    /** Email */
    email!: string;

    /** Права доступа пользователя */
    roles!: string[];

    country: number | undefined;
}