namespace Lox
{
    sealed class GroupingExpression : IExpression
    {
        public IExpression Expression { get; }

        public GroupingExpression(IExpression expression)
        {
            Expression = expression;
        }

        public SyntaxKind Kind => SyntaxKind.GroupingExpression;
    }
}
