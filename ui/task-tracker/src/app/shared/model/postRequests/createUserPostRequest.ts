export class AuthenticatePostRequest {
    lastName!: string;
    firstName! : string;
    email! : string;
    country : number | undefined;
    password! : string;
}
