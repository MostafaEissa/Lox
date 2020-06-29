using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Lox
{
    sealed class Parser
    {
        private IList<Token> _tokens;
        private List<Error> _errors = new List<Error>();

        private int _current = 0;


        public Parser(IList<Token> tokens)
        {
            _tokens = tokens;
        }

        public List<SyntaxNode> Parse()
        {
            var stmts = new List<SyntaxNode>();
            while (!IsAtEnd())
            {
                stmts.Add(ParseDeclaration());
            }

            return stmts;
        }

        public IEnumerable<Error> GetErrors()
        {
            return _errors;
        }

        private SyntaxNode ParseStatement()
        {
            
            if (Match(TokenType.Print)) return ParsePrintStatement();
            if (Match(TokenType.If)) return ParseIfStatement();
            if (Match(TokenType.While)) return ParseWhileStatement();
            if (Match(TokenType.For)) return ParseForStatement();
            if (Match(TokenType.LeftBrace)) return ParseBlockStatement();

            //TODO: add more statement types
            return ParseExpressionStatement();
        }


        private SyntaxNode ParseDeclaration()
        {
            if (Match(TokenType.Var)) return ParseVariableDeclaration();
            return ParseStatement();
        }


        private SyntaxNode ParseVariableDeclaration()
        {
            Consume(TokenType.Identifier, "Expect variable name.");
            Token name = Previous();

            SyntaxNode initializer = null;
            if (Match(TokenType.Equal)) initializer = ParseExpression();

            Consume(TokenType.Semicolon, "Expect ; after variable declaration");
            return new VariableDeclarationStatement(name, initializer);
        }

        private SyntaxNode ParseBlockStatement()
        {
            var statements = new List<SyntaxNode>();

            while(!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                statements.Add(ParseDeclaration());
            }

            Consume(TokenType.RightBrace, "Expect '}' after block.");
            return new BlockStatement(statements);
        }

        private SyntaxNode ParseForStatement()
        {
            Consume(TokenType.LeftParen, "expect'(' after 'for'.");
            SyntaxNode initializer;
            if (Match(TokenType.Semicolon)) initializer = null;
            else if (Match(TokenType.Var)) initializer = ParseVariableDeclaration();
            else initializer = ParseExpressionStatement();

            SyntaxNode condition = null;
            if (!Check(TokenType.Semicolon)) condition = ParseExpression();
            Consume(TokenType.Semicolon, "expect';' after loop condition.");

            SyntaxNode increment = null;
            if (!Check(TokenType.RightParen)) increment = ParseExpression();
            Consume(TokenType.RightParen, "expect ')' after for clauses.");

            SyntaxNode body = ParseStatement();

            // desugar into a while loop
            if (increment != null)
                body = new BlockStatement(new List<SyntaxNode>{body, new ExpressionStatement(increment)});

            if (condition == null) condition = new LiteralExpression(true);
            body = new WhileStatement(condition, body);

            if (initializer != null)
                body = new BlockStatement(new List<SyntaxNode>{new ExpressionStatement(initializer), body});

            return body;
            
        }
        private SyntaxNode ParseWhileStatement()
        {
            Consume(TokenType.LeftParen, "expect'(' after 'while'.");
            SyntaxNode condition = ParseExpression();
            Consume(TokenType.RightParen, "expect ')' after condition.");

            SyntaxNode body = ParseStatement();
            return new WhileStatement(condition, body);
        }

        private SyntaxNode ParseIfStatement()
        {
            Consume(TokenType.LeftParen, "expect'(' after if.");
            SyntaxNode condition = ParseExpression();
            Consume(TokenType.RightParen, "expect ')' after if condition.");

            SyntaxNode thenBranch = ParseStatement();
            SyntaxNode elseBranch = null;
            if (Match(TokenType.Else))
                elseBranch = ParseStatement();

            return new IfStatement(condition, thenBranch, elseBranch );
        }

        private SyntaxNode ParsePrintStatement()
        {
            SyntaxNode expr = ParseExpression();
            Consume(TokenType.Semicolon, "Expect ; after expression");
            return new PrintStatement(expr);
        }
        private SyntaxNode ParseExpressionStatement()
        {
            SyntaxNode expr = ParseExpression();
            Consume(TokenType.Semicolon, "Expect ; after expression");
            return new ExpressionStatement(expr);
        }

        private SyntaxNode ParseExpression()
        {
            return ParseAssignmentExpression();
        }

        private SyntaxNode ParseAssignmentExpression()
        {
            SyntaxNode expr = ParseBinaryExpression();

            if (Match(TokenType.Equal))
            {
                Token equals = Previous();
                SyntaxNode value = ParseAssignmentExpression();

                if (expr is VariableExpression)
                {
                    Token name = ((VariableExpression)expr).Name;
                    return new AssignmentExpression(name, value);
                }

                Error(equals, "Invalid Assignment Target");
            }

            return expr;
        }

        private SyntaxNode ParseBinaryExpression(int parentPrecedence = 0)
        {
            SyntaxNode left;

            var unaryPrecedence = Peek().Type.GetUnaryOperatorPrecendence();
            if (unaryPrecedence != 0 && unaryPrecedence >= parentPrecedence)
            {
                Token oper = Advance();
                SyntaxNode right = ParseBinaryExpression(unaryPrecedence);
                left = new UnaryExpression(oper, right);
            }
            else
            {
                left = ParsePrimaryExpression();
            }

            while(true)
            {
                var binaryPrecedence = Peek().Type.GetBinaryOperatorPrecendence();
                if (binaryPrecedence != 0 && binaryPrecedence > parentPrecedence)
                {
                    Token oper = Advance();
                    SyntaxNode right = ParseBinaryExpression(binaryPrecedence);
                    left = new BinaryExpression(left, oper, right);
                }
                else
                {
                    break;
                }
            }
          

            return left;
        }

        private SyntaxNode ParsePrimaryExpression()
        {
            switch(Peek().Type)
            {
                case TokenType.False:
                    Match(TokenType.False);
                    return new LiteralExpression(false);
                case TokenType.True:
                    Match(TokenType.True);
                    return new LiteralExpression(true);
                case TokenType.Nil:
                    Match(TokenType.Nil);
                    return new LiteralExpression(null);
                case TokenType.Number:
                case TokenType.String:
                    Match(TokenType.Number, TokenType.String);
                    return new LiteralExpression(Previous().Literal);
                case TokenType.Identifier:
                    Match(TokenType.Identifier);
                    return new VariableExpression(Previous());
                case TokenType.LeftParen:
                    SyntaxNode expr = ParseExpression();
                    Consume(TokenType.RightParen, "Expect ')' after expression");
                    return new GroupingExpression(expr);

            }

            Error(Peek(), "Expect Expression");

            //TODO: fix this
            return null;
        }

        private bool Match(params TokenType[] types)
        {
            foreach(var type in types)
            {
                if(Check(type))
                {
                    Advance();
                    return true;
                }
            }
            return false;
        }

        private bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;
            return Peek().Type == type;
        }

        private Token Advance()
        {
            if (!IsAtEnd()) _current++;
            return Previous();
        }

        private bool IsAtEnd()
        {
            return Peek().Type == TokenType.Eof;
        }

        private Token Peek()
        {
            return _tokens[_current];
        }

        private Token Previous()
        {
            return _tokens[_current - 1];
        }

        private void Consume(TokenType type, string message)
        {
            if (Check(type)) Advance();
            else
            {
                Error(Peek(), message);
                Synchronize();
            }
        }

        private void Error(Token token, string message)
        {
            if (token.Type == TokenType.Eof)
                _errors.Add(new Error(ErrorType.SyntaxError, token.Line, " at end", message));
            else
                _errors.Add(new Error(ErrorType.SyntaxError, token.Line, $" at '{token.Lexeme}'", message));
        }

        private void Synchronize()
        {
            Advance();

            while(!IsAtEnd())
            {
                if (Previous().Type == TokenType.Semicolon) return;

                switch (Peek().Type)
                {
                    case TokenType.Class:
                    case TokenType.Fun:
                    case TokenType.Var:
                    case TokenType.If:
                    case TokenType.While:
                    case TokenType.For:
                    case TokenType.Return:
                        return;
                }

                Advance();
            }
        }
    }
}
