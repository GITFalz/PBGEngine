using System.Text.RegularExpressions;

namespace PBG.Parse;

public static class Rx
{
    public static string Lit(string s) => Regex.Escape(s);
    public static string Space => @"\s+";
    public static string OptSpace => @"\s*";
    public static string Word => @"\w+";
    public static string Capture(string pattern) => $"({pattern})";
    public static string Start => @"^";
    public static string End => @"$";
    public static string Or(params string[] alts) => "(?:" + string.Join("|", alts.Select(Regex.Escape)) + ")";
    public static string Optional(string pattern) => $"(?:{pattern})?";

    public static string Seq(params string[] parts) => string.Concat(parts);

    public static Regex Compile(string pattern, RegexOptions opt = RegexOptions.Compiled) => new Regex(pattern, opt);
}