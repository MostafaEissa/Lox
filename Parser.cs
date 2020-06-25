using System;
using System.Collections.Generic;

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

        public IExpression Parse()
        {
            return ParseExpression();
        }

        public IEnumerable<Error> GetErrors()
        {
            return _errors;
        }

        private IExpression ParseExpression()
        {
            return ParseBinaryExpression();
        }

        private IExpression ParseBinaryExpression(int parentPrecedence = 0)
        {
            IExpression left;

            var unaryPrecedence = Peek().Type.GetUnaryOperatorPrecendence();
            if (unaryPrecedence != 0 && unaryPrecedence >= parentPrecedence)
            {
                Token oper = Advance();
                IExpression right = ParseBinaryExpression(unaryPrecedence);
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
                    IExpression right = ParseBinaryExpression(binaryPrecedence);
                    left = new BinaryExpression(left, oper, right);
                }
                else
                {
                    break;
                }
            }
          

            return left;
        }

        private IExpression ParsePrimaryExpression()
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
                case TokenType.LeftParen:
                    IExpression expr = ParseExpression();
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
            else Error(Peek(), message);
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
                    case TokenType.Let:
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
