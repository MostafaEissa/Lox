using System;
using System.Reflection.Metadata;

namespace Lox
{
    class RuntimeError : Exception
    {
        public Token Token {get;}
        public RuntimeError(Token token, string message) : base(message)
        {
            Token = token;
        }
    }
    class Evaluator 
    {
        public object Evaluate(IExpression expression)
        {
            switch (expression.Kind) 
            {
                case SyntaxKind.BinaryExpression:
                    return EvaluateBinaryExpression((BinaryExpression)expression);
                case SyntaxKind.GroupingExpression:
                    return EvaluateGroupingExpression((GroupingExpression)expression);
                case SyntaxKind.UnaryExpression:
                    return EvaluateUnaryExpression((UnaryExpression)expression);
                case SyntaxKind.LiteralExpression:
                    return EvaluateLiteralExpression((LiteralExpression)expression);
                default:
                    throw new NotSupportedException();
            }
        }

        private object EvaluateLiteralExpression(LiteralExpression expr)
        {
            return expr.Value.Or(null);
        }

        private object EvaluateGroupingExpression(GroupingExpression expr)
        {
            return Evaluate(expr.Expression);  
        }

        private object EvaluateUnaryExpression(UnaryExpression expr)
        {
            var right = Evaluate(expr.Right);
            switch (expr.Operator.Type)
            {
                case TokenType.Minus:
                    CheckNumberOperand(expr.Operator, right);
                    return - (double)right;
                case TokenType.Bang:
                return !IsTruthy(right);
            }
            throw new NotSupportedException($"Unexpected unary operator {expr.Operator}");
        }

        private bool IsTruthy(object obj)
        {
            if (obj is null) return false;
            if (obj is bool) return (bool)obj;
            return true;
        }

        private object EvaluateBinaryExpression(BinaryExpression expr)
        {
            object left = Evaluate(expr.Left);
            object right = Evaluate(expr.Right);

            switch (expr.Operator.Type)
            {
                case TokenType.Minus:
                    checkNumberOperands(expr.Operator, left, right);
                    return (double)left - (double)right;
                case TokenType.Star:
                    checkNumberOperands(expr.Operator, left, right);
                    return (double)left * (double)right;
                case TokenType.Slash:
                    checkNumberOperands(expr.Operator, left, right);
                    return (double)left / (double)right;
                case TokenType.Plus:
                    if (left is double && right is double)
                        return (double)left + (double)right;
                    else if (left is double && right is double)
                        return (string)left + (string)right;
                    else
                        throw new RuntimeError(expr.Operator, "Operands must be two numbers or two strings.");
                case TokenType.Greater:
                    checkNumberOperands(expr.Operator, left, right);
                    return (double)left > (double)right;
                case TokenType.GreaterEqual:
                    checkNumberOperands(expr.Operator, left, right);
                    return (double)left >= (double)right;
                case TokenType.Less:
                    checkNumberOperands(expr.Operator, left, right);
                    return (double)left < (double)right;
                case TokenType.LessEqual:
                    checkNumberOperands(expr.Operator, left, right);
                    return (double)left <= (double)right;
                case TokenType.EqualEqual:
                    return Equals(left, right);
                case TokenType.BangEqual:
                    return !Equals(left, right);
            }

            throw new NotSupportedException($"Unexpected binary operator {expr.Operator}");
        }

        private void CheckNumberOperand(Token oper, Object operand) 
        {
            if (operand is double) return;
            throw new RuntimeError(oper, $"Operand must be a number.");
        }

        private void checkNumberOperands(Token oper, Object left, Object right) 
        {
            if (left is Double && right is Double) return; 
            throw new RuntimeError(oper, "Operands must be numbers.");
        }

    }
}
