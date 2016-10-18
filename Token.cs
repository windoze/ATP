using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Marketplace.ARMTemplate.Common.Models
{
    public enum TokenTypes
    {
        None,
        Identifier,
        Number,
        String,
        Lbrace,
        Rbrace,
        Lbracket,
        Rbracket,
        Comma,
        Dot
    }

    public class Token
    {
        public Token(TokenTypes type, string s)
        {
            Type = type;
            Content = s;
        }

        public TokenTypes Type { get; }
        public string Content { get; }
    }

    public class Tokenizer
    {
        private static readonly Regex IdentifierRegex = new Regex(@"[A-Za-z_][A-Za-z0-9_]*");
        private static readonly Regex NumberRegex = new Regex(@"[-+]?[0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?");

        private readonly string Content;
        private int _currentPos = 0;

        public Tokenizer(string s)
        {
            Content = s;
        }

        static bool IsWhitespace(char c)
        {
            return (c == ' ') || (c == '\t');
        }

        static bool IsAlpha(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        static bool IsNumber(char c)
        {
            return c >= '0' && c <= '9';
        }

        string GetIdentifier()
        {
            var match = IdentifierRegex.Match(Content, _currentPos);
            if (match.Success)
            {
                _currentPos += match.Length;
                return match.Value;
            }
            // TODO: Exception
            return "";
        }

        string GetNumber()
        {
            var match = NumberRegex.Match(Content, _currentPos);
            if (match.Success)
            {
                _currentPos += match.Length;
                return match.Value;
            }
            // TODO: Exception
            return "";
        }

        string GetString()
        {
            // TODO: Get string and update CurrentPos, consume trailing '"'
            _currentPos++; // Skip leading '\''
            int start = _currentPos; // Save start pos
            var sb = new StringBuilder();
            bool ended = false;
            while (_currentPos < Content.Length)
            {
                char c = Content[_currentPos];
                if (c == '\'')
                {
                    // Skip trailing '\''
                    _currentPos++;
                    ended = true;
                    break;
                }
                // Looks ARM template doesn't support escape sequence?
                //                else if (c == '\\')
                //                {
                //                    // Escape sequence
                //                    _currentPos++;
                //                    switch (Content[_currentPos])
                //                    {
                //                        default:
                //                            break;
                //                    }
                //                }
                sb.Append(c);
                _currentPos++;
            }
            if (!ended)
            {
                // TODO: Exception, no closing quote
            }
            return sb.ToString();
        }

        public bool Completed => _currentPos >= Content.Length;

        public Token NextToken()
        {
            while (_currentPos < Content.Length)
            {
                if (IsWhitespace(Content[_currentPos]))
                {
                    _currentPos++;
                }
                else
                {
                    break;
                }
            }
            if (_currentPos < Content.Length)
            {
                char c = Content[_currentPos];
                switch (c)
                {
                    case '.':
                        _currentPos++;
                        return new Token(TokenTypes.Dot, ".");
                    case ',':
                        _currentPos++;
                        return new Token(TokenTypes.Comma, ",");
                    case '(':
                        _currentPos++;
                        return new Token(TokenTypes.Lbrace, "(");
                    case ')':
                        _currentPos++;
                        return new Token(TokenTypes.Rbrace, ")");
                    case '[':
                        _currentPos++;
                        return new Token(TokenTypes.Lbracket, "[");
                    case ']':
                        _currentPos++;
                        return new Token(TokenTypes.Rbracket, "]");
                    case '\'':
                        // TODO: String
                        return new Token(TokenTypes.String, GetString());
                    default:
                        if (IsAlpha(c))
                        {
                            // TODO: Identifier
                            return new Token(TokenTypes.Identifier, GetIdentifier());
                        }
                        else if (IsNumber(c))
                        {
                            return new Token(TokenTypes.Number, GetNumber());
                        }
                        break;
                }
            }
            return new Token(TokenTypes.None, "");
        }

        public Token LookAhead()
        {
            int savedPos = _currentPos;
            var t = NextToken();
            _currentPos = savedPos;
            return t;
        }
    }
}
