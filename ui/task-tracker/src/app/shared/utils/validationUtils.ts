import { IValidationResult } from "../interfaces/validationResult";
import { RegexUtils } from "./regexUtils";

export class ValidationUtils {
    private static requiredError: string = "All fields are required";
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
                errorText: "Password must contain at least one capital letter in Latin"
            };
        }

        if (!/[a-zа-яё]/.test(password)) {
            return {
                isValid: false,
                errorText: "Password must contain at least one lowercase letter in Latin"
            };
        }

        if (/[А-ЯЁ]/.test(password) || /[а-яё]/.test(password)) {
            return {
                isValid: false,
                errorText: "Password can not contain any characters in Cyrillic"
            };
        }

        if (!/\d/.test(password)) {
            return {
                isValid: false,
                errorText: "Password must contain at least one number"
            };
        }

        if (password.length < 6) {
            return {
                isValid: false,
                errorText: "Password must be longer than 5 characters"
            };
        }

        if (password.length > 100) {
            return {
                isValid: false,
                errorText: "Password must be less than 100 characters"
            };
        }

        if (/^[\w\-\s]+$/.test(password)) {
            return {
                isValid: false,
                errorText: "Password must contain at least one alphanumeric character"
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
                errorText: "Password confirmation does not match password"
            }
        }

        return {
            isValid: true,
            errorText: ""
        };
    }
}
