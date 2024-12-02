/**
 * Результат валидации
 */
export interface IValidationResult {
    /**
     * Валидно/невалидно
     */
    isValid: boolean;
    /**
     * Текст сообщения об ошибке
     */
    errorText: string;
}