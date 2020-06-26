using static Lox.Functional;

namespace Lox
{
    sealed class LiteralExpression : IExpression
    {
        public Option<object> Value { get; }

        public LiteralExpression(object? value)
        {
            Value = value ?? None;
        }

        public SyntaxKind Kind => SyntaxKind.LiteralExpression;
    }
}
