namespace Lox
{
    sealed class BinaryExpression : IExpression
    {
        public IExpression Left { get; }
        public IExpression Right { get; }
        public Token Operator { get; }

        public BinaryExpression(IExpression left, Token oper, IExpression right)
        {
            Left = left;
            Operator = oper;
            Right = right;
        }

        public SyntaxKind Kind => SyntaxKind.BinaryExpression;
    }
}
