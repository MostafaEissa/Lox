using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text.Json.Serialization;
using static Lox.Functional;

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

    class Environment
    {
        private Dictionary<String, Object?> _values = new Dictionary<String, object?>();

        private Environment? _enclosing;

        public Environment(Environment? enclosing = null)
        {
            _enclosing = enclosing;
        }


        public void Define(String name, object? value)
        {
            _values[name] =  value;
        }

        public object Get(Token name)
        {
            if (_values.TryGetValue(name.Lexeme, out object? value))
                return value ?? None;

            if (_enclosing != null) return _enclosing.Get(name);

            throw new RuntimeError(name, $"Undefined variable {name.Lexeme}.");
        }

       
        public void Assign(Token name, object? value)
        {
            if (_values.ContainsKey(name.Lexeme))
                _values[name.Lexeme] = value;

            else if (_enclosing != null)
                _enclosing.Assign(name, value);

            else
                throw new RuntimeError(name, $"Undefined variable {name.Lexeme}.");
        }
    }

    class Evaluator
    {
        private Environment _environment = new Environment();
        public void Evaluate(List<SyntaxNode> expressions)
        {
            foreach (var expression in expressions)
            {
                Evaluate(expression);
            }
        }

        private object Evaluate(SyntaxNode expression)
        {
            switch (expression.Kind)
            {
                case SyntaxKind.PrintStatement:
                    var result = Evaluate(((PrintStatement)expression).Expression);
                    Console.WriteLine(result);
                    return System.ValueTuple.Create();
                case SyntaxKind.ExpressionStatement:
                    Evaluate(((ExpressionStatement)expression).Expression);
                    return System.ValueTuple.Create();
                case SyntaxKind.VariableDeclarationStatement:
                    return EvaluateVariableDeclartionStatement((VariableDeclarationStatement)expression);
                case SyntaxKind.BlockStatement:
                    return EvaluateBlockStatement((BlockStatement)expression);
                case SyntaxKind.VariableExpression:
                    return EvaluateVariableExpression((VariableExpression)expression);
                case SyntaxKind.AssignmentExpression:
                    return EvaluateAssignmentExpression((AssignmentExpression)expression);
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

        private object EvaluateBlockStatement(BlockStatement expr)
        {
            EvaluateBlock(expr.Statements, new Environment(_environment));
            return null;
        }

        private void EvaluateBlock(List<SyntaxNode> statements, Environment environment)
        {
            var previous = _environment;
            try
            {
                this._environment = environment;
                foreach (var statement in statements)
                    Evaluate(statement);
            }
            finally
            {
                this._environment = previous;
            }
        }

        private object EvaluateAssignmentExpression(AssignmentExpression expr)
        {
            var value = Evaluate(expr.Value);
            _environment.Assign(expr.Name, value);
            return value;
        }
        private object EvaluateVariableDeclartionStatement(VariableDeclarationStatement expr)
        {
            object value = null;
            if (expr.Initializer != null)
                value = Evaluate(expr.Initializer);

            _environment.Define(expr.Name.Lexeme, value);
            return null;
        }

        private object EvaluateVariableExpression(VariableExpression expr)
        {
            return _environment.Get(expr.Name);
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
