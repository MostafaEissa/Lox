namespace Lox
{
    sealed class Error
    {
        public int Line { get; }
        public string Message { get; }

        public ErrorType Type { get; }

        public Error(ErrorType type, int line, string message)
        {
            this.Type = type;
            this.Line = Line;
            this.Message = message;
        }
    }
}
