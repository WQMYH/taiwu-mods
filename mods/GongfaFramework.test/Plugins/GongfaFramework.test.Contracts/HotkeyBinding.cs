using System;

namespace GongfaFramework.Test.Contracts;

[Serializable]
public sealed class ParsedHotkey
{
    public bool Valid;
    public bool Ctrl;
    public bool Alt;
    public bool Shift;
    public string Key = "";
    public string Normalized = "";
    public string Error = "";

    public static ParsedHotkey Parse(string text)
    {
        var result = new ParsedHotkey();
        if (string.IsNullOrWhiteSpace(text))
        {
            result.Error = "快捷键不能为空。";
            return result;
        }
        foreach (string raw in text.Split('+'))
        {
            string part = raw.Trim();
            if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                part.Equals("Control", StringComparison.OrdinalIgnoreCase)) result.Ctrl = true;
            else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase)) result.Alt = true;
            else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase)) result.Shift = true;
            else if (result.Key.Length == 0) result.Key = part;
            else
            {
                result.Error = "只能指定一个主键。";
                return result;
            }
        }
        if (result.Key.Length == 0)
        {
            result.Error = "缺少主键。";
            return result;
        }
        result.Valid = true;
        result.Normalized = (result.Ctrl ? "Ctrl+" : "") + (result.Alt ? "Alt+" : "") +
                            (result.Shift ? "Shift+" : "") + result.Key;
        return result;
    }
}
