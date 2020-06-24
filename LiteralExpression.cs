namespace Lox
{
    sealed class LiteralExpression : IExpression
    {
        public object Value { get; }

        public LiteralExpression(object value)
        {
            Value = value;
        }

        public SyntaxKind Kind => SyntaxKind.LiteralExpression;
    }
}
