using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text.Json.Serialization;
using static Lox.Functional;

namespace Lox
{

    interface LoxCallable
    {
        object Call(Evaluator evaluator, List<object> arguments);
        int Arity {get;}
    }

    class Return : Exception
    {
        public object Value {get;}
        public Return(object value) 
        {
            Value = value;
        }
    }

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

        public object GetAt(int distance, Token name)
        {
            return Ancestor(distance).Get(name);
        }

        private Environment Ancestor(int distance)
        {
            Environment env = this;
            for (int i = 0; i < distance; i++)
                env = env._enclosing;
            
            return env;
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

        public void AssignAt(int distance, Token name, object? value)
        {
            Ancestor(distance).Assign(name, value);
        }
    }

    class Evaluator
    {
        public Environment Globals {get;}
        private Environment _environment;
        private Dictionary<SyntaxNode, int> _locals = new Dictionary<SyntaxNode, int>();

        public Evaluator()
        {
            Globals = new Environment();
            _environment = Globals;
        }

        public void Resolve(SyntaxNode expr, int depth)
        {
            _locals[expr] = depth;
        }

        private object LookupVariable(Token name, SyntaxNode expr)
        {
            var res = _locals.TryGetValue(expr, out int distance);
            if (res) return _environment.GetAt(distance, name);
            return Globals.Get(name);
        }
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
                case SyntaxKind.IfStatement:
                    return EvaluateIfStatement((IfStatement)expression);
                case SyntaxKind.WhileStatement:
                    return EvaluateWhileStatement((WhileStatement)expression);
                case SyntaxKind.BlockStatement:
                    return EvaluateBlockStatement((BlockStatement)expression);
                case SyntaxKind.FunctionStatement:
                    return EvaluateFunctionStatement((FunctionStatement)expression);
                case SyntaxKind.ReturnStatement:
                    return EvaluateReturnStatement((ReturnStatement)expression);
                case SyntaxKind.CallExpression:
                    return EvaluateCallExpression((CallExpression)expression);
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

        private object EvaluateReturnStatement(ReturnStatement expr)
        {
           
            object value = null;
            if (expr.Value != null) value = Evaluate(expr.Value);
            throw new Return(value);
        }
        private object EvaluateFunctionStatement(FunctionStatement expr)
        {
            var function = new LoxFunction(expr, _environment);
            _environment.Define(expr.Name.Lexeme, function);
            return null;
        }
        private object EvaluateCallExpression(CallExpression expr)
        {
            var callee = Evaluate(expr.Callee);
            var arguments = new List<Object>();
            foreach(var argument in expr.Arguments)
            {
                arguments.Add(Evaluate(argument));
            }
            var function = callee as LoxCallable;
            if (function is null) 
            {
                throw new RuntimeError(expr.Paren, "Can olny call functions and classes.");
            }

            // check arity
            if (arguments.Count != function.Arity)
            {
                throw new RuntimeError(expr.Paren, $"Expected {function.Arity} arguments but got {arguments.Count}.");
            }
            return function.Call(this, arguments);
        }
        private object EvaluateWhileStatement(WhileStatement expr)
        {
            while (IsTruthy(Evaluate(expr.Condition)))
                Evaluate(expr.Body);
            
            return null;
        }

        private object EvaluateIfStatement(IfStatement expr)
        {
            if (IsTruthy(Evaluate(expr.Condition)))
                Evaluate(expr.ThenBranch);
            else if (expr.ElseBranch != null)
                Evaluate(expr.ElseBranch);
            
            return null;
        }

        private object EvaluateBlockStatement(BlockStatement expr)
        {
            EvaluateBlock(expr.Statements, new Environment(_environment));
            return null;
        }

        public void EvaluateBlock(List<SyntaxNode> statements, Environment environment)
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
            var res = _locals.TryGetValue(expr, out var distance);
            if (res) _environment.AssignAt(distance, expr.Name, value);
            else Globals.Assign(expr.Name, value);
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
            return LookupVariable(expr.Name, expr);
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
            if (obj is null || obj == None) return false;
            if (obj is bool) return (bool)obj;
            return true;
        }

        private object EvaluateBinaryExpression(BinaryExpression expr)
        {
            object left = Evaluate(expr.Left);
            object right = null;
            if (!(expr.Operator.Type == TokenType.AndAnd || expr.Operator.Type == TokenType.OrOr))
                right = Evaluate(expr.Right);

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
                case TokenType.AndAnd:
                    if (!IsTruthy(left)) return left;
                    else return Evaluate(expr.Right);
                case TokenType.OrOr:
                    if (IsTruthy(left)) return left;
                    else return Evaluate(expr.Right);
                

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
