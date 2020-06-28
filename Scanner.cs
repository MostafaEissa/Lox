using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Runtime.CompilerServices;
using static Lox.Functional;

namespace Lox
{
    sealed class Scanner
    {
        private string _source;
        private List<Token> _tokens = new List<Token>();
        private List<Error> _errors = new List<Error>();

        private int _start = 0;
        private int _current = 0;
        private int _line = 1;

        internal static readonly Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>
        {
            {"if",     TokenType.If},
            {"else",   TokenType.Else},
            {"true",   TokenType.True},
            {"false",  TokenType.False},
            {"for",    TokenType.For},
            {"while",  TokenType.While},
            {"fun",    TokenType.Fun},
            {"nil",    TokenType.Nil},
            {"return", TokenType.Return},
            {"class",  TokenType.Class},
            {"this",   TokenType.This},
            {"super",  TokenType.Super},
            {"let",    TokenType.Let},
            {"print",  TokenType.Print },
           
        };

        public IEnumerable<Token> GetTokens()
        {
            return _tokens;
        }

        public IEnumerable<Error> GetErrors()
        {
            return _errors;
        }

        public Scanner(string source)
        {
            _source = source;

        }

        public void ScanTokens()
        {
            
            while (!IsAtEnd)
            {
                _start = _current;
                ScanToken();
            }

            _tokens.Add(new Token(TokenType.Eof, "", None, _line));
        }

        private bool IsAtEnd => _current >= _source.Length;

        private void ScanToken()
        {
            char c = Advance();
            switch(c)
            {
                case '(': AddToken(TokenType.LeftParen); break;
                case ')': AddToken(TokenType.RightParen); break;
                case '{': AddToken(TokenType.LeftBrace); break;
                case '}': AddToken(TokenType.RightBrace); break;
                case ',': AddToken(TokenType.Comma); break;
                case '.': AddToken(TokenType.Dot); break;
                case '-': AddToken(TokenType.Minus); break;
                case '+': AddToken(TokenType.Plus); break;
                case ';': AddToken(TokenType.Semicolon); break;
                case '*': AddToken(TokenType.Star); break;

                case '!': AddToken(Match('=') ? TokenType.BangEqual : TokenType.Bang); break;
                case '=': AddToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal); break;
                case '<': AddToken(Match('=') ? TokenType.LessEqual : TokenType.Less); break;
                case '>': AddToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater); break;
                case '&': AddToken(Match('&') ? TokenType.AndAnd : TokenType.And); break;
                case '|': AddToken(Match('|') ? TokenType.OrOr : TokenType.Or); break;

                case '/':
                    if (Match('/')) //comment
                    {
                        while (Peek() != '\n' && !IsAtEnd) Advance();
                    }
                    else
                    {
                        AddToken(TokenType.Slash);
                    }
                    break;

                // ignore whitespace
                case ' ':
                case '\r':
                case '\t':
                    break;

                case '\n':
                    _line++;
                    break;

                case '"': String(); break;
                
                default:
                    if (IsDigit(c))
                    {
                        Number();
                    }
                    else if (IsAlpha(c))
                    {
                        Identifier();
                    }
                    else
                    {
                        Error(_line, "Unexpected character.");
                    }
                    break;
            }

        }

        private char Advance()
        {
            _current++;
            return _source[_current - 1];
        }

        private bool Match(char expected)
        {
            if (IsAtEnd) return false;

            if (_source[_current] != expected) return false;

            // consume character in case it matches
            _current++;
            return true;
        }

        private char Peek(int num = 0)
        {
            switch(num)
            {
                case 0:
                    if (IsAtEnd) return '\0';
                    return _source[_current];
                case 1:
                    if (_current + 1 >= _source.Length) return '\0';
                    return _source[_current + 1];
                default:
                    throw new NotSupportedException($"Look Ahead of {num} is not supported.");
            }
           
        }

        private void AddToken(TokenType type)
        {
            AddToken(type, None);
        }

        private void AddToken(TokenType type, object literal)
        {
            string text = _source.Substring(_start, _current - _start);
            _tokens.Add(new Token(type, text, literal, _line));
        }

        private void Error(int line, string message)
        {
            _errors.Add(new Error(ErrorType.SyntaxError, line, message));
        }

        private void String()
        {
            while (Peek() != '"' && ! IsAtEnd)
            {
                if (Peek() == '\n') _line++;
                Advance();
            }

            // unterminated string
            if (IsAtEnd)
            {
                Error(_line, "Unterminated string.");
                return;
            }

            //the closing "
            Advance();

            string value = _source.Substring(_start + 1, _current - _start  - 2 /*remove quotes*/);
            AddToken(TokenType.String, value);
        }

        private void Number()
        {
            while(IsDigit(Peek())) Advance();
            

            if (Peek() == '.' && IsDigit(Peek(1)))
            {
                Advance();
                while (IsDigit(Peek())) Advance();
            }

            AddToken(TokenType.Number, Double.Parse(_source.Substring(_start, _current - _start)));
        }

        private void Identifier()
        {
            while (IsAlphaNumeric(Peek())) Advance();

            var text = _source.Substring(_start, _current - _start);
            if (Keywords.TryGetValue(text, out TokenType tokenType))
                AddToken(tokenType);
            else
                AddToken(TokenType.Identifier);
        }

        private bool IsAlpha(char c) =>    (c >= 'a' && c <= 'z') ||   (c >= 'A' && c <= 'Z') ||  c == '_'; 

        private bool IsDigit(char c) => c >= '0' && c <= '9';

        private bool IsAlphaNumeric(char c) => IsAlpha(c) || IsDigit(c);
    }
}
