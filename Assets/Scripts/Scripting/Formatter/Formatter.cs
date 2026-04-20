using PBG.MathLibrary;
using PBG.UI;

namespace PBG.Compiler;

public class Formatter
{
    public int LineIndex = 0;
    
    public void Format(List<UIText> texts)
    {
        for (int i = 0; i < texts.Count; i++)
        {
            LineIndex = i;
            var text = texts[i];
            FormatLine(text, text.GetText());
        }
    }

    public void FormatLine(UIText text) => FormatLine(text, text.GetText());
    public void FormatLine(UIText text, string line)
    {
        int trailingSpaces = line.TakeWhile(char.IsWhiteSpace).Count();
        line = line.Trim();

        if (string.IsNullOrEmpty(line))
            return;

        var newLine = GameCompiler.SpaceTokens(line);
        newLine = newLine.Replace("(", "( ");
        newLine = newLine.Replace("-", " -");
        newLine = CompilerFormater.MyRegex().Replace(newLine, " ");
        
        // Split the line into multiple parts
        var Parts = newLine.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (Parts.Length == 0)
            return;

        List<Token> tokens = [];

        var index = 0;
        for (int j = 0; j < Parts.Length; j++)
        {
            var p = Parts[j].Trim();
            if (p.Length == 0)
                continue;

            while (index < line.Length && line[index] != p[0])
            {
                index++;
            }

            tokens.Add(new Token()
            {
                Line = p,
                IndexStart = index + trailingSpaces,
                Count = p.Length
            });

            index += p.Length;
        }

        FormatTokens(text, tokens);
    }

    public void FormatTokens(UIText text, List<Token> tokens)
    {
        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (Keywords.TryGetValue(token, out var type))
            {
                var color = TokenColors[type];
                text.GetTextMesh().SetCharactersColor(text, color, token.IndexStart, token.Count);
                continue;
            }

            if (Operators.Contains(token))
            {
                var color = TokenColors[CodeTokenType.Operator];
                text.GetTextMesh().SetCharactersColor(text, color, token.IndexStart, token.Count);
                continue;
            }

            if (Parenthesis.Contains(token))
            {
                var color = TokenColors[CodeTokenType.Parenthesis];
                text.GetTextMesh().SetCharactersColor(text, color, token.IndexStart, token.Count);
                continue;
            }

            if (Parse.Float.TryParse(token, out _))
            {
                var color = TokenColors[CodeTokenType.Number];
                text.GetTextMesh().SetCharactersColor(text, color, token.IndexStart, token.Count);
                continue;
            }
            
            if (token.Line.EndsWith('(') && Functions.Contains(token.Line[..^1]))
            {
                text.GetTextMesh().SetCharactersColor(text, TokenColors[CodeTokenType.Function], token.IndexStart, token.Count - 1);
                text.GetTextMesh().SetCharactersColor(text, TokenColors[CodeTokenType.Parenthesis], token.IndexStart + (token.Count - 1), 1);
                continue;
            }

            text.GetTextMesh().SetCharactersColor(text, TokenColors[CodeTokenType.Variable], token.IndexStart, token.Count);
        }
    }

    public static readonly Dictionary<string, CodeTokenType> Keywords = new()
    {
        { "if", CodeTokenType.Keyword },
        { "then", CodeTokenType.Keyword },
        { "else", CodeTokenType.Keyword },
        { "end", CodeTokenType.Keyword },
        { "return", CodeTokenType.Keyword },

        { "float", CodeTokenType.Type },
        { "int", CodeTokenType.Type },
        { "bool", CodeTokenType.Type },
    };

    public static readonly HashSet<string> Functions = [
        "abs",
        "floor",
        "ceil",
        "round",
        "sqrt",
        "pow",
        "log",
        "log2",
        "exp",
        "min",
        "max",
        "clamp",
        "sin",
        "cos",
        "tan",
        "asin",
        "acos",
        "atan",
        "atan2",
        "radians",
        "degrees",
        "lerp",
        "sign",
        "fract",
        "mod",
        "distance"
    ];

    public static readonly HashSet<string> Operators =
    [
        "+", "-", "*", "/", "=",
        ">", "<", ">=", "<=",
        "==", "!=", "+=", "-=", 
        "||", "&&"
    ];

    public static readonly HashSet<string> Parenthesis =
    [
        "(",
        ")",
        "{",
        "}",
        "[",
        "]"
    ];

    public static readonly Dictionary<CodeTokenType, Vector3> TokenColors = new()
    {
        { CodeTokenType.Default,     new Vector3(0.84f, 0.84f, 0.84f) },  // #D4D4D4 light gray
        { CodeTokenType.Keyword,     new Vector3(0.34f, 0.61f, 0.84f) },  // #569CD6 blue
        { CodeTokenType.Type,        new Vector3(0.27f, 0.75f, 0.72f) },  // #4EC9B0 teal
        { CodeTokenType.Number,      new Vector3(0.71f, 0.81f, 0.66f) },  // #B5CEA8 soft green
        { CodeTokenType.Variable,    new Vector3(0.61f, 0.82f, 0.99f) },  // #9CDCFE light blue
        { CodeTokenType.Function,    new Vector3(0.86f, 0.86f, 0.67f) },  // #DCDCAA yellow
        { CodeTokenType.Operator,    new Vector3(0.84f, 0.84f, 0.84f) },  // #D4D4D4 same as default
        { CodeTokenType.Parenthesis, new Vector3(0.98f, 0.84f, 0.36f) },  // #FFD700 gold/yellow
    };

    public enum CodeTokenType
    {
        Default,
        Keyword,
        Type,
        Number,
        Variable,
        Function,
        Operator,
        Parenthesis,
        Comment
    }
}