export { };

declare global {
    interface String {
        isNullOrWhitespace(): boolean;
    }
}