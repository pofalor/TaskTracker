import { DataError } from "./dataError";

export interface IResponse<T> {
  data?: (T);
  errors: DataError[];
}
