export interface IResponse<T> {
  data?: (T);
  errorCode: number;
  errorMsgs: string[];
}
