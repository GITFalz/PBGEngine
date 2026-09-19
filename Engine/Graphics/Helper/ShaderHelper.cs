using System.Text.RegularExpressions;
using PBG.Parse;

namespace PBG.Graphics;

[InternalSystemInit(InitPriority.EngineCore)]
public static class ShaderHelper
{
    //\s*(?:((?:flat|noperspective|smooth|centroid)\s+))?(\w+)\s+(\w+)\s*;\s*
    private static Regex _passRegex = Rx.Compile(Rx.Seq(
        Rx.Start,
        Rx.OptSpace,
        Rx.Optional(Rx.Capture(Rx.Seq(
            Rx.Or("flat", "noperspective", "smooth", "centroid"),
            Rx.Space
        ))),
        Rx.Capture(Rx.Word),
        Rx.Space,
        Rx.Capture(Rx.Word),
        Rx.OptSpace,
        Rx.Lit(";")
    ));

    //\s*const\s+(\w+)\[(\w+)\]\s+(\w+)\s+=\s+\(
    private static Regex _arrayRegex = Rx.Compile(Rx.Seq(
        Rx.Start,
        Rx.OptSpace,
        Rx.Lit("const"),
        Rx.Space,
        Rx.Capture(Rx.Word),
        Rx.OptSpace,
        Rx.Lit("["),
        Rx.OptSpace,
        Rx.Capture(Rx.Word),
        Rx.OptSpace,
        Rx.Lit("]"),
        Rx.OptSpace,
        Rx.Capture(Rx.Word),
        Rx.OptSpace,
        Rx.Lit("=")
    ));

    public static void Init()
    {
        #if DEBUG
        Console.WriteLine($"[DEBUG - INFO] : Pass regex : " + _passRegex.ToString());
        #endif
    }

    public static string BaseShaderPath(string file) => Game.ShaderPath / file;
    public static string FixedShaderPath(string file) => Game.FixedShaderPath / (file + ".fixed");

    public static bool FixVertexShader(string vertexShaderPath, string fixedVertexShaderPath)
    {
        if (!File.Exists(vertexShaderPath))
        {
            Console.WriteLine($"[ERROR] : vertex shader at '{vertexShaderPath}' not found while trying to fix it");
            return false;
        }

        ContentType contentType = ContentType.Default;
        int passCount = 0;
    
        var lines = File.ReadAllLines(vertexShaderPath);
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line == "#pass")
            {
                lines[i] = "";
                contentType = ContentType.Pass;
                continue;
            }

            if (line == "#end")
            {
                lines[i] = "";
                contentType = ContentType.Default;
                continue;
            }

            if (contentType == ContentType.Pass)
            {
                if (HandlePassRegex(ref line, ref passCount, PassType.Out))
                {
                    lines[i] = line;
                    passCount++;
                    continue;
                }
            }

            if (HandleArrayRegex(ref line))
            {
                lines[i] = line;
                continue;
            }
        }

        File.WriteAllLines(fixedVertexShaderPath, lines);

        return true;
    }

    public static bool FixVertexAndFragmentShader(string vertexShaderPath, string fixedVertexShaderPath, string fragmentShaderPath, string fixedFragmentShaderPath)
    {
        if (!File.Exists(vertexShaderPath) || !File.Exists(fragmentShaderPath))
        {
            if (!File.Exists(vertexShaderPath)) Console.WriteLine($"[ERROR] : vertex shader at '{vertexShaderPath}' not found while trying to fix it");
            if (!File.Exists(fragmentShaderPath)) Console.WriteLine($"[ERROR] : fragment shader at '{fragmentShaderPath}' not found while trying to fix it");
            return false;
        }

        ContentType contentType = ContentType.Default;
        int passCount = 0;
    
        var vertLines = File.ReadAllLines(vertexShaderPath);
        for (int i = 0; i < vertLines.Length; i++)
        {
            var line = vertLines[i].Trim();

            if (line == "#pass")
            {
                vertLines[i] = "";
                contentType = ContentType.Pass;
                continue;
            }

            if (line == "#end")
            {
                vertLines[i] = "";
                contentType = ContentType.Default;
                continue;
            }

            if (contentType == ContentType.Pass)
            {
                if (HandlePassRegex(ref line, ref passCount, PassType.Out))
                {
                    vertLines[i] = line;
                    passCount++;
                    continue;
                }
            }

            if (HandleArrayRegex(ref line))
            {
                vertLines[i] = line;
                continue;
            }
        }

        contentType = ContentType.Default;
        passCount = 0;
        bool fragColor = false;

        var fragLines = File.ReadAllLines(fragmentShaderPath).ToList();
        for (int i = 0; i < fragLines.Count; i++)
        {
            var line = fragLines[i].Trim();
            if (line == "#pass")
            {
                fragLines[i] = "";
                contentType = ContentType.Pass;
                continue;
            }

            if (line == "#end")
            {
                fragLines[i] = "";
                contentType = ContentType.Default;
                continue;
            }

            if (!fragColor && line.Contains("out vec4 FragColor;"))
            {
                fragColor = true;
            }

            if (contentType == ContentType.Pass)
            {
                if (HandlePassRegex(ref line, ref passCount, PassType.In))
                {
                    fragLines[i] = line;
                    passCount++;
                    continue;
                }
            }

            if (HandleArrayRegex(ref line))
            {
                fragLines[i] = line;
                continue;
            }
        }

        if (!fragColor)
        {
            fragLines.Insert(1, "layout(location = 0) out vec4 FragColor;");
        }

        File.WriteAllLines(fixedVertexShaderPath, vertLines);
        File.WriteAllLines(fixedFragmentShaderPath, fragLines);

        #if DEBUG
        Console.WriteLine($"[INFO] : Fixed vertex shader at '{vertexShaderPath}'");
        Console.WriteLine($"[INFO] : Fixed fragment shader at '{fragmentShaderPath}'");
        #endif

        return true;
    }

    private static bool HandlePassRegex(ref string line, ref int location, PassType passType)
    {
        var m = _passRegex.Match(line);

        if (!m.Success)
            return false;

        string qual = m.Groups[1].Success ? $" {m.Groups[1].Value.Trim()}" : "";
        string pass = passType == PassType.In ? "in" : "out";
        string type = m.Groups[2].Value;
        string name = m.Groups[3].Value;
        
        line = $"layout(location = {location}){qual} {pass} {type} {name};";

        switch (type)
        {
            case "double3":
            case "double4":
            case "dvec3":
            case "dvec4":
                location += 1;
                break;

            case "mat2":
                location += 1;
                break;

            case "mat3":
                location += 2;
                break;

            case "mat4":
                location += 3;
                break;

            case "mat2x3":
                location += 1;
                break;

            case "mat3x2":
                location += 2;
                break;

            case "mat2x4":
                location += 1;
                break;

            case "mat4x2":
                location += 3;
                break;

            case "mat3x4":
                location += 2;
                break;

            case "mat4x3":
                location += 3;
                break;
        }
        return true;
    }

    private static bool HandleArrayRegex(ref string line)
    {
        var m = _arrayRegex.Match(line);

        if (!m.Success)
            return false;

        string type = m.Groups[1].Value;
        string count = m.Groups[2].Value;
        string name = m.Groups[3].Value;
        
        string replacement = $"const {type} {name}[{count}] = {type}[]";

        line = line[..m.Index] + replacement + line[(m.Index + m.Length)..];

        return true;
    }

    private enum ContentType
    {
        Default,
        Pass,
    }

    private enum PassType
    {
        In,
        Out
    }
}