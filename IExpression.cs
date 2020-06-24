namespace Lox
{
    interface IExpression
    {
        SyntaxKind Kind { get; }
    }
}
