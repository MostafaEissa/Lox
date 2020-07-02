using System;
using System.Collections.Generic;

namespace Lox
{
    sealed class Resolver 
    {
        enum FunctionType 
        {
            None,
            Function,
        }
        private Evaluator _evaluator;
        private List<Dictionary<string, bool>> _scopes = new List<Dictionary<string, bool>>();

        private FunctionType _currentFunction = FunctionType.None;

          private List<Error> _errors = new List<Error>();

        public IEnumerable<Error> GetErrors()
        {
            return _errors;
        }

        private void Error(Token token, string message)
        {
            if (token.Type == TokenType.Eof)
                _errors.Add(new Error(ErrorType.SyntaxError, token.Line, " at end", message));
            else
                _errors.Add(new Error(ErrorType.SyntaxError, token.Line, $" at '{token.Lexeme}'", message));
        }

        public Resolver(Evaluator evaluator)
        {
            _evaluator = evaluator;
        }
         public void Resolve(List<SyntaxNode> expressions)
        {
            foreach (var expression in expressions)
            {
                Resolve(expression);
            }
        }


        private void Resolve(SyntaxNode expression)
        {
            switch (expression.Kind)
            {
                 case SyntaxKind.BlockStatement:
                    ResolveBlockStatement((BlockStatement)expression);
                    break;
                case SyntaxKind.VariableDeclarationStatement:
                    ResolveVariableDeclarationStatement((VariableDeclarationStatement)expression);
                    break;
                case SyntaxKind.VariableExpression:
                    ResolveVariableExpression((VariableExpression)expression);
                    break;
                 case SyntaxKind.AssignmentExpression:
                    ResolveAssignmentExpression((AssignmentExpression)expression);
                    break;
                case SyntaxKind.FunctionStatement:
                    ResolveFunctionStatement((FunctionStatement)expression);
                    break;
                case SyntaxKind.ExpressionStatement:
                    Resolve(((ExpressionStatement)expression).Expression);
                    break;
                case SyntaxKind.IfStatement:
                    ResolveIfStatement((IfStatement)expression); 
                    break;
                case SyntaxKind.PrintStatement:
                    Resolve(((PrintStatement)expression).Expression);
                    break;
                case SyntaxKind.ReturnStatement:
                    ResolveReturnStatement((ReturnStatement)expression);
                    break;
                case SyntaxKind.WhileStatement:
                    ResolveWhileStatement((WhileStatement)expression);
                    break;
                case SyntaxKind.BinaryExpression:
                    ResolveBinaryExpression((BinaryExpression)expression);
                    break;
                case SyntaxKind.CallExpression:
                    ResolveCallExpression((CallExpression)expression);
                    break;
                case SyntaxKind.GroupingExpression:
                    Resolve(((GroupingExpression)expression).Expression);
                    break;
                case SyntaxKind.LiteralExpression:
                    break;
                case SyntaxKind.UnaryExpression:
                    ResolveUnaryExpression((UnaryExpression)expression);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private void BeginScope()
        {
            _scopes.Add(new Dictionary<string, bool>());
        }

        private void EndScope()
        {
            _scopes.RemoveAt(_scopes.Count - 1);
        }

        private void Declare(Token name)
        {
            if (_scopes.Count == 0) return;
            var scope = _scopes[_scopes.Count - 1];
            if (scope.ContainsKey(name.Lexeme))
                Error(name, "Variable with this name already declared in this scope.");
            scope[name.Lexeme] =  false;
        }

        private void Define(Token name)
        {
            if (_scopes.Count == 0) return;
            var scope = _scopes[_scopes.Count - 1];
            scope[name.Lexeme]= true;
        }

        private void ResolveBlockStatement(BlockStatement expr)
        {
             BeginScope();
             Resolve(expr.Statements);
             EndScope();
        }

        private void ResolveVariableDeclarationStatement(VariableDeclarationStatement expr)
        {
            Declare(expr.Name);
            if (expr.Initializer != null)
                Resolve(expr.Initializer);
            Define(expr.Name);
        }

        private void ResolveVariableExpression(VariableExpression expr)
        {
            if (_scopes.Count != 0)
            {
                if (_scopes[_scopes.Count - 1].TryGetValue(expr.Name.Lexeme, out var value) && value == false)
                {
                    Error(expr.Name, "cannot read local variable in its own initalizer");
                }
            } 
            ResolveLocal(expr, expr.Name);
        }

        private void ResolveLocal(SyntaxNode expr, Token name)
        {
            for (int i = _scopes.Count - 1; i >= 0; i-- )
            {
                if (_scopes[i].ContainsKey(name.Lexeme))
                {
                    _evaluator.Resolve(expr, _scopes.Count - 1 - i);
                    break;
                }
            }
        }

        private void ResolveAssignmentExpression(AssignmentExpression expr)
        {
            Resolve(expr.Value);
            ResolveLocal(expr, expr.Name);
        }

        private void ResolveFunctionStatement(FunctionStatement expr)
        {
            Declare(expr.Name);
            Define(expr.Name);
            ResolveFunction(expr, FunctionType.Function);
        }

        private void ResolveFunction(FunctionStatement expr, FunctionType type)
        {
            var enclosingFunction = _currentFunction;
            _currentFunction = type;

            BeginScope();
            foreach( var param in expr.Parameters)
            {
                Declare(param);
                Define(param);
            }
            Resolve(expr.Body);
            EndScope();

            _currentFunction = enclosingFunction;
        }

        private void ResolveIfStatement(IfStatement expr)
        {
            Resolve(expr.Condition);
            Resolve(expr.ThenBranch);
            if (expr.ElseBranch != null) Resolve(expr.ElseBranch);
        }

        private void ResolveReturnStatement(ReturnStatement expr)
        {
             if (_currentFunction == FunctionType.None)
                Error(expr.Keyword, "cannot return from top level code");
            if (expr.Value != null)
                Resolve(expr.Value);
        }

        private void ResolveWhileStatement(WhileStatement expr)
        {
            Resolve(expr.Condition);
            Resolve(expr.Body);
        }

        private void ResolveBinaryExpression(BinaryExpression expr)
        {
            Resolve(expr.Left);
            Resolve(expr.Right);
        }

        private void ResolveCallExpression(CallExpression expr)
        {
            Resolve(expr.Callee);
            foreach(var arg in expr.Arguments)
            {
                Resolve(arg);
            }
        }

        private void ResolveUnaryExpression(UnaryExpression expr)
        {
            Resolve(expr.Right);
        }
    }
}
