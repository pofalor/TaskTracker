import { IValidationResult } from "../interfaces/validationResult";
import { RegexUtils } from "./regexUtils";

export class ValidationUtils {
    private static requiredError: string = "errors.required";
    /**
     * Проверка что электронная почта валидна.
     * В случае невалидности возвращает IValidationResult с текстом ошибки.
     */
    public static validateEmail(val: string): IValidationResult {

        if (val.isNullOrWhitespace()) {
            return {
                isValid: false,
                errorText: this.requiredError
            };
        }

        if (!RegexUtils.isValidEmail(val)) {
            return {
                isValid: false,
                errorText: 'errors.invalid'
            }
        }

        return {
            isValid: true,
            errorText: ""
        };
    }

    /** валидация пароля */
    public static validatePassword(password: string): IValidationResult {
        if (password.isNullOrWhitespace()) {
            return {
                isValid: false,
                errorText: this.requiredError
            };
        }

        if (!/[A-ZА-ЯЁ]/.test(password)) {
            return {
                isValid: false,
                errorText: "errors.password.atLeastOneUpperCase"
            };
        }

        if (!/[a-zа-яё]/.test(password)) {
            return {
                isValid: false,
                errorText: "errors.password.atLeastOneLowerCase"
            };
        }

        if (/[А-ЯЁ]/.test(password) || /[а-яё]/.test(password)) {
            return {
                isValid: false,
                errorText: "errors.password.noCyrillic"
            };
        }

        if (!/\d/.test(password)) {
            return {
                isValid: false,
                errorText: "errors.password.atLeastOneDigit"
            };
        }

        if (password.length < 6) {
            return {
                isValid: false,
                errorText: "errors.password.minLength"
            };
        }

        if (password.length > 100) {
            return {
                isValid: false,
                errorText: "errors.password.maxLength"
            };
        }

        if (/^[\w\-\s]+$/.test(password)) {
            return {
                isValid: false,
                errorText: "errors.password.alphaNumeric"
            };
        }

        return {
            isValid: true,
            errorText: ""
        };
    }

    /** валидация подтверждения пароля */
    public static validatePasswordConfirmation(password: string, confirmation: string): IValidationResult {
        if (confirmation.isNullOrWhitespace()) {
            return {
                isValid: false,
                errorText: this.requiredError
            };
        }

        if (password !== confirmation) {
            return {
                isValid: false,
                errorText: "errors.passwordConfirmation.notMatch"
            }
        }

        return {
            isValid: true,
            errorText: ""
        };
    }

    /** валидация даты */
    public static validateDate(date: any): IValidationResult {
        var today = new Date();
        if (date > today || date == "Invalid Date") {
            return {
                isValid: false,
                errorText: "errors.futureDate"
            }
        }
        return {
            isValid: true,
            errorText: ""
        };
    }
}
