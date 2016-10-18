using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marketplace.ARMTemplate.Common.Models
{
    public enum ExpressionTypes
    {
        String,
        Number,
        Function,
    }

    public class ExpressionException : Exception
    {
        public ExpressionException(string message) : base(message)
        {
        }
    }

    public enum IndexType
    {
        Key,
        Index
    }

    public class Subindex
    {
        public Subindex(string s)
        {
            Type = IndexType.Key;
            Key = s;
        }

        public Subindex(Expression e)
        {
            Type = IndexType.Index;
            Index = e;
        }

        public IndexType Type { get; }
        public string Key { get; }
        public Expression Index { get; }
    }

    public class Expression
    {
        private string _content;

        public Expression(string s)
        {
            Children = new List<Expression>();
            Subindices = new List<Subindex>();
            if (s.Length >= 2 && s[0] == '[' && s[s.Length - 1] == ']')
            {
                Parse(new Tokenizer(s.Substring(1, s.Length - 2)));
            }
            else
            {
                try
                {
                    var t = JToken.Parse(s);
                    SetContent(t);
                }
                catch
                {
                    SetContent(s);
                }

            }
        }

        private Expression(Tokenizer lexer)
        {
            Children = new List<Expression>();
            Subindices = new List<Subindex>();
            Parse(lexer);
        }


        public string Content
        {
            get { return _content; }
            set { Parse(value); }
        }

        public void SetContent(JToken t)
        {
            switch (t.Type)
            {
                case JTokenType.Integer:
                    Type = ExpressionTypes.Number;
                    _content = t.Value<int>().ToString();
                    break;
                default:
                    Type = ExpressionTypes.String;
                    _content = t.Value<string>();
                    break;
            }
        }

        public void SetContent(string t)
        {
            Type = ExpressionTypes.String;
            _content = t;
        }

        public IEnumerable<Expression> PostOrderTraversal()
        {
            foreach (var c in Children)
            {
                foreach (var n in c.PostOrderTraversal())
                    yield return n;
            }
            yield return this;
        }

        public JToken Evaluate()
        {
            // TODO: Evaluate expression
            return Content;
        }

        public ArmTemplate Root { get; }

        public override string ToString()
        {
            return Evaluate().ToString();
        }

        private void Parse(string s)
        {
            Children.Clear();
            Subindices.Clear();
            if (s.Length >= 2 && s[0] == '[' && s[s.Length - 1] == ']')
            {
                Parse(new Tokenizer(s.Substring(1, s.Length - 2)));
            }
            else
            {
                var t = JToken.Parse(s);
                SetContent(t);
            }
        }

        private void Parse(Tokenizer lexer)
        {
            var tok = lexer.NextToken();
            if (tok.Type == TokenTypes.Number)
            {
                // Simple value
                Type = ExpressionTypes.Number;
                _content = tok.Content;
                return;
            }

            if (tok.Type == TokenTypes.String)
            {
                // Simple value
                Type = ExpressionTypes.String;
                _content = tok.Content;
                return;
            }

            if (tok.Type == TokenTypes.Identifier)
            {
                // Function call
                _content = tok.Content;
                tok = lexer.NextToken();
                if (tok.Type != TokenTypes.Lbrace)
                {
                    throw new ExpressionException("Syntax error");
                }
                bool closed = false;
                while (!lexer.Completed)
                {
                    tok = lexer.LookAhead();
                    if (tok.Type == TokenTypes.Rbrace)
                    {
                        // Consume the ')'
                        lexer.NextToken();
                        closed = true;
                        break;
                    }
                    // This is an oprand
                    Children.Add(new Expression(lexer));
                    if (lexer.LookAhead().Type == TokenTypes.Comma)
                    {
                        // Consume next comma
                        lexer.NextToken();
                    }
                }
                if (!closed)
                {
                    throw new ExpressionException("Syntax error");
                }

                // Look ahead for trailing properties
                if (lexer.LookAhead().Type == TokenTypes.Dot)
                {
                    // There are trailing properties
                    while (!lexer.Completed)
                    {
                        if (lexer.LookAhead().Type == TokenTypes.Dot)
                        {
                            // Property "x.y"
                            // Consume '.'
                            lexer.NextToken();
                            tok = lexer.NextToken();
                            if (tok.Type != TokenTypes.Identifier)
                            {
                                throw new ExpressionException("Syntax error");
                            }
                            Subindices.Add(new Subindex(tok.Content));
                        }
                        else if (lexer.LookAhead().Type == TokenTypes.Lbracket)
                        {
                            // Array Index
                            // Consume '['
                            lexer.NextToken();
                            var idx = new Expression(lexer);
                            Subindices.Add(new Subindex(idx));
                            // Consume and check ']'
                            if (lexer.NextToken().Type != TokenTypes.Rbracket)
                            {
                                throw new ExpressionException("Syntax error");
                            }
                        }
                        else
                        {
                            // properties list ended
                            break;
                        }
                    }
                }
                Type = ExpressionTypes.Function;
                return;
            }

            throw new ExpressionException("Syntax error");
        }

        public string DebugDump(int indent = 0)
        {
            var sb = new StringBuilder();
            sb.Append(' ', indent * 4);
            sb.Append(Content);
            if (Children.Count > 0)
                sb.Append('\n');
            foreach (var c in Children)
            {
                sb.Append(c.DebugDump(indent + 1));
                sb.Append('\n');
            }
            if (Subindices.Count > 0)
            {
                sb.Append(' ', indent * 4);
                foreach (var p in Subindices)
                {
                    sb.Append('.');
                    sb.Append(p);
                }
            }
            return sb.ToString();
        }

        public string Dump()
        {
            switch (Type)
            {
                case ExpressionTypes.Number:
                    return Content;
                case ExpressionTypes.String:
                    return '"' + Content + '"';
                case ExpressionTypes.Function:
                    var sb = new StringBuilder(Content);
                    sb.Append('(');
                    bool first = true;
                    foreach (var c in Children)
                    {
                        if (!first)
                        {
                            sb.Append(',');
                        }
                        else
                        {
                            first = false;
                        }
                        sb.Append(c.Dump());
                    }
                    sb.Append(')');
                    foreach (var i in Subindices)
                    {
                        switch (i.Type)
                        {
                            case IndexType.Index:
                                sb.Append('[');
                                sb.Append(i.Index.Dump());
                                sb.Append(']');
                                break;
                            case IndexType.Key:
                                sb.Append('.');
                                sb.Append(i.Key);
                                break;
                        }
                    }
                    return sb.ToString();
            }
            return null;
        }

        public ExpressionTypes Type { get; set; }
        public List<Expression> Children { get; }
        public List<Subindex> Subindices { get; } // Subindices list, i.e. "reference("some").prop1.prop2"
    }
}
