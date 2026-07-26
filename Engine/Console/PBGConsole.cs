using System.Collections.Frozen;
using PBG.Core;
using PBG.Data;
using PBG.Graphics;
using PBG.UI;
using PBG.Voxel;
using static PBG.UI.Styles;

namespace PBG.PBGConsole;

[InternalSystemInit(InitPriority.Data)]
public class PBGConsole : ScriptingNode
{
    private static List<string> CommandHistory = [];
    private static int HistoryIndex = 0;

    private static bool _memoryLine = false;

    private UIController uIContoller;

    private UIElementBase _baseUI = null!;
    private UIVScroll _commandHistory = null!;
    private UIField _commandField = null!;

    public static bool Focused = false;

    private static bool _started = false;

    private Dictionary<string, CommandModule> Commands = new()
    {
        {
            "time", 
            new CommandModule("time", [
                new CommandModule("set", null, TimeSetAction),
                new CommandModule("add", null, TimeAddAction),
                new CommandModule("speed", null, TimeSpeedAction),
                new CommandModule("pause", null, TimePauseAction),
                new CommandModule("resume", null, TimeResumeAction)
            ])
        },
        {
            "debug", 
            new CommandModule("debug", [
                new CommandModule("chunks", null, _ => 
                {
                    WorldSettings.ShowChunkDebug = !WorldSettings.ShowChunkDebug;
                    return new(true, "Toggled chunk debugging");
                }),
            ])
        }
    };

    private static CommandResult TimeSetAction(CommandContext context)
    {
        if (!context.HasToken())
            return new(false, "Expected code after \"" + context.LastToken() + "\"");

        var token = context.CurrentToken();
        float value = Parse.Float.Parse(token);
        
        WorldSettings.SetTime(value);

        return new(true, "Set time to " + value + " successfully");
    }

    private static CommandResult TimeAddAction(CommandContext context)
    {
        if (!context.HasToken())
            return new(false, "Expected code after \"" + context.LastToken() + "\"");

        var token = context.CurrentToken();
        float value = Parse.Float.Parse(token);
        
        WorldSettings.AddTime(value);

        return new(true, "Set time to " + value + " successfully");
    }

    private static CommandResult TimeSpeedAction(CommandContext context)
    {
        if (!context.HasToken())
            return new(false, "Expected code after \"" + context.LastToken() + "\"");

        var token = context.CurrentToken();
        float value = Parse.Float.Parse(token);
        
        WorldSettings.SetDaySpeed(value);

        if (value == 0)
            return new(true, "Time speed cannot be 0, so it has been paused");
        return new(true, "Set time speed to " + value + " successfully");
    }

    private static CommandResult TimePauseAction(CommandContext context)
    {
        WorldSettings.Pause();
        return new(true, "Paused time successfully");
    }

    private static CommandResult TimeResumeAction(CommandContext context)
    {
        WorldSettings.Resume();
        return new(true, "Resumed time successfully");
    }

    public static void Focus()
    {
        Scene.CurrentScene.DefaultCamera.Freeze();
        Focused = true;
    }

    public static void Unfocus()
    {
        Scene.CurrentScene.DefaultCamera.Unfreeze();
        Focused = false;
    }

    void Start()
    {
        var controller = Transform.GetComponent<UIController>();
        _baseUI = GetUI();
        controller.AddElement(_baseUI);
        uIContoller = controller;

        if (!_started)
        {
            string historyPath = Game.DataPath / "console" / "history.txt";

            Directory.CreateDirectory(Path.GetDirectoryName(historyPath)!);

            CommandHistory = [.. File.ReadAllLines(historyPath)];

            _started = true;
        }
    }

    void Awake()
    {
        Unfocus();
    }

    void Update()
    {
        if (Input.IsKeyPressed(Key.Enter))
        {
            // Execute code
            var command = _commandField.GetTrimmedText();

            if (_memoryLine)
            {
                CommandHistory.RemoveAt(0);
                _memoryLine = false;
            }

            CommandHistory.Insert(0, command);

            // To be organised
            var tokens = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            CommandResult result = new(false, "Command failed");
            if (tokens.Length > 0)
            {
                if (Commands.TryGetValue(tokens[0], out var commandModule))
                {
                    CommandContext commandContext = new()
                    {
                        CurrentScene = Scene.CurrentScene,
                        Tokens = tokens,
                        TokenIndex = 1
                    };
                    result = commandModule.Run(ref commandContext);
                }
                else
                {
                    result.Reason = "Unknown command \"" + tokens[0] + "\"";
                }
            }
            else
            {
                result.Reason = "Empty command line";
            }

            UIText newLine = new UIText(result.Reason, top_left, fs_[1.2f]);

            if (_commandHistory.ChildElements.Count == 0)
            {
                _commandHistory.SetVisible(true);
            }

            if (_commandHistory.ChildElements.Count == 50)
            {
                _commandHistory.ChildElements[0].Delete();
            }

            _commandHistory.AddElement(newLine);
            uIContoller.AddElement(newLine);

            _baseUI.ApplyChanges(UIChange.Scale);

            _commandField.UpdateText("");
        }

        if (Focused)
        {
            if (Input.IsKeyPressed(Key.Up))
            {
                if (!_memoryLine)
                {
                    CommandHistory.Insert(0, _commandField.GetTrimmedText());
                    _memoryLine = true;
                }
                else if (HistoryIndex == 0)
                {
                    CommandHistory[0] = _commandField.GetTrimmedText();
                }

                if (HistoryIndex < CommandHistory.Count - 1)
                {
                    HistoryIndex++;
                    _commandField.UpdateText(CommandHistory[HistoryIndex]);
                }
            }

            if (Input.IsKeyPressed(Key.Down))
            {
                if (HistoryIndex > 0)
                {
                    HistoryIndex--;
                    _commandField.UpdateText(CommandHistory[HistoryIndex]);
                }
            }
        }
    }

    void Exit()
    {
        Unfocus();
    }

    public static void Save()
    {
        if (_memoryLine)
        {
            CommandHistory.RemoveAt(0);
            _memoryLine = false;
        }

        File.WriteAllLines(Game.DataPath / "console" / "history.txt", CommandHistory);
    }

    private UIElementBase GetUI() => 
    new UIVCol(bottom_left, w_[800], min_h_[30], spacing_[-2], blank_round, ignore_invisible, rgba_[0, 0, 0, 0.3f], border_ui_[2, 2, 2, 2], border_color_[(0.3f, 0.3f, 0.3f, 0.3f)], left_[10], bottom_[10], grow_children)[
        new UIVScroll(hidden, border_[10, 10, 10, 10], blank_full, border_ui_[0, 0, 0, 2], border_color_[(0.3f, 0.3f, 0.3f, 0.3f)], spacing_[10], top_left, w_full, grow_children, max_h_[500], mask_children).Ref(ref _commandHistory),
        new UIHScroll(top_left, w_[800], h_[30], mask_children, blank_round)[
            new UIField("", mc_[1000], middle_left, left_[10], fs_[1.2f]).Ref(ref _commandField).OnClick(_ => Focus()).OnTextEnter(_ => Unfocus())
        ]
    ];
}

public struct CommandContext
{
    public Scene CurrentScene;
    public string[] Tokens;
    public int TokenIndex;
    
    public bool HasToken() => TokenIndex < Tokens.Length;
    public string CurrentToken() => Tokens[TokenIndex];
    public string LastToken() => Tokens[TokenIndex-1];
}

public class CommandModule
{
    public string Name;
    public Func<CommandContext, CommandResult>? Action = null;
    public FrozenDictionary<string, CommandModule>? Children;

    public CommandModule(string name, CommandModule[]? children, Func<CommandContext, CommandResult>? action = null)
    {
        Name = name;
        Action = action;
        Children = children?.ToFrozenDictionary(c => c.Name);
    }

    public CommandResult Run(ref CommandContext context)
    {
        if (!context.HasToken())
        {
            if (Action != null)
                return Action.Invoke(context);

            return new(false, "Expected code after \"" + context.LastToken() + "\"");
        }

        var token = context.CurrentToken();

        if (Children != null && Children.TryGetValue(token, out var module))
        {
            context.TokenIndex++;

            return module.Run(ref context);
        }
        else
        {
            return Action?.Invoke(context) ?? new(false, "Unknown command \"" + token + "\"");
        }
    }
}

public struct CommandResult
{
    public bool Result = false;
    public string Reason = "";
    public CommandResult(bool result, string reason) { Result = result; Reason = reason; }
}