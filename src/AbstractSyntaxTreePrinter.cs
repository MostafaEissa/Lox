﻿using System;
using System.Text;

namespace Lox
{
    class AbstractSyntaxTreePrinter
    {
        public string Print(SyntaxNode expr)
        {
            switch(expr)
            {
                case BinaryExpression e:
                    return Parenthesize(e.Operator.Lexeme, e.Left, e.Right);

                case GroupingExpression e:
                    return Parenthesize("group", e.Expression);

                case UnaryExpression e:
                    return Parenthesize(e.Operator.Lexeme, e.Right);

                case LiteralExpression e:
                    return e.Value.Map(v => v.ToString()!).Or("Nil");

                default:
                    return String.Empty;
            }
        }

        private string Parenthesize(string name, params SyntaxNode[] exprs)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("(").Append(name);
            foreach (var expr in exprs)
            {
                builder.Append(" ");
                builder.Append(Print(expr));
            }
            builder.Append(")");

            return builder.ToString();
        }
    }
}
