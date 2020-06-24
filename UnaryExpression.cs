namespace Lox
{
    sealed class UnaryExpression : IExpression
    {
        public IExpression Right { get; }
        public Token Operator { get; }

        public UnaryExpression(Token oper, IExpression right)
        {
            Operator = oper;
            Right = right;
        }

        public SyntaxKind Kind => SyntaxKind.UnaryExpression;
    }
}
