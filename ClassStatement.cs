using System.Collections.Generic;

namespace Lox
{
    class ClassStatement : SyntaxNode 
    {
        public Token Name {get;}
        public List<FunctionStatement> Methods {get;}

        public SyntaxKind Kind => SyntaxKind.ClassStatement;

        public ClassStatement(Token name, List<FunctionStatement> methods)
        {
            Name = name;
            Methods = methods;
        }
    }
}
