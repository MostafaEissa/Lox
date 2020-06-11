namespace Lox
{
    enum TokenType
    {
        // single character tokens
        LeftParen,
        RightParen,
        LeftBrace,
        RightBrace,
        Comma,
        Dot, 
        Minus,
        Plus,
        Semicolon,
        Slash,
        Star,

        // one or two character tokens
        Bang, 
        BangEqual,
        Equal,
        EqualEqual,
        Greater,
        GreaterEqual,
        Less,
        LessEqual,
        And,
        AndAnd,
        Or,
        OrOr,

        // literals
        Identifier,
        String,
        Number,

        // keywords
        False,
        True,
        Else,
        If,
        Fun,
        Return,
        For,
        While,
        Nil,
        Class,
        This,
        Super,
        Let,

        // end of file
        Eof
    }
}
