
using Silk.NET.Input;

namespace PBG.UI
{
    public static class Char
    {
        private static readonly Dictionary<int, char> AzertyChar = new Dictionary<int, char>()
        {
            { 32, ' '},
            { 49, '&' },
            { 1049, '1' },
            { 50, 'é' },
            { 1050, '2' },
            { 2050, '~'},
            { 51, '"' },
            { 1051, '3' },
            { 2051, '#' },
            { 52, '\'' },
            { 1052, '4' },
            { 2052, '{' },
            { 53, '(' },
            { 1053, '5' },
            { 2053, '[' },
            { 54, '-' },
            { 1054, '6' },
            { 2054, '|' },
            { 55, 'è' },
            { 1055, '7' },
            { 2055, '`' },
            { 56, '_' },
            { 1056, '8' },
            { 2056, '\\' },
            { 57, 'ç' },
            { 1057, '9' },
            { 2057, '^' },
            { 48, 'à' },
            { 1048, '0' },
            { 2048, '@' },
            { 45, ')' },
            { 1045, '°' },
            { 2045, ']' },
            { 61, '=' },
            { 1061, '+' },
            { 2061, '}' },

            { 81, 'a' },
            { 66, 'b' },
            { 67, 'c' },
            { 68, 'd' },
            { 69, 'e' },
            { 70, 'f' },
            { 71, 'g' },
            { 72, 'h' },
            { 73, 'i' },
            { 74, 'j' },
            { 75, 'k' },
            { 76, 'l' },
            { 59, 'm' },
            { 78, 'n' },
            { 79, 'o' },
            { 80, 'p' },
            { 65, 'q' },
            { 82, 'r' },
            { 83, 's' },
            { 84, 't' },
            { 85, 'u' },
            { 86, 'v' },
            { 90, 'w' },
            { 88, 'x' },
            { 89, 'y' },
            { 87, 'z' },

            { 1081, 'A' },
            { 1066, 'B' },
            { 1067, 'C' },
            { 1068, 'D' },
            { 1069, 'E' },
            { 1070, 'F' },
            { 1071, 'G' },
            { 1072, 'H' },
            { 1073, 'I' },
            { 1074, 'J' },
            { 1075, 'K' },
            { 1076, 'L' },
            { 1059, 'M' },
            { 1078, 'N' },
            { 1079, 'O' },
            { 1080, 'P' },
            { 1065, 'Q' },
            { 1082, 'R' },
            { 1083, 'S' },
            { 1084, 'T' },
            { 1085, 'U' },
            { 1086, 'V' },
            { 1090, 'W' },
            { 1088, 'X' },
            { 1089, 'Y' },
            { 1087, 'Z' },

            { 161, '<'},
            { 1161, '>'},
            { 91, '^'},
            { 1091, '¨'},
            { 93, '$'},
            { 1093, '£'},
            { 39, 'ù'},
            { 1039, '%'},
            { 92, '*'},
            { 1092, 'µ'},
            { 77, ','},
            { 1077, '?'},
            { 44, ';'},
            { 1044, '.'},
            { 46, ':'},
            { 1046, '/'},
            { 47, '!'},
            { 1047, '§'}
        };

        private static Dictionary<int, char> _keyboardLayout = AzertyChar;

        public static bool GetChar(out char c, Key key, bool shift = false, bool alt = false)
        {
            c = '\0';
            int index = (int)key + (shift ? 1000 : 0) + (alt ? 2000 : 0);
            return _keyboardLayout.TryGetValue(index, out c);
        }

        public static void SetKeyboardLayout(KeyboardLayout layout)
        {
            if (layout == KeyboardLayout.Azerty)
                _keyboardLayout = AzertyChar;
        }
    }

    public enum KeyboardLayout
    {
        Azerty = 0,
        Qwerty = 1,
    }
}