String.prototype.isNullOrWhitespace = function(this: string) : boolean{
    return this == null || /^\s*$/.test(this);
}
