/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
using System.Globalization;
using System.Windows.Data;

namespace RegStr;

/// <summary>
/// TextBox に入っているレジスタ値と RegStrItem.Value の相互変換を行うコンバーター。
/// 接頭辞が無い場合は 10 進数として扱う。
/// </summary>
public class RegValueConverter : IValueConverter
{
    // ulong -> string (0x 表記)
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value == null ? null : $"0x{(ulong)value:x16}";
    }

    // string (符号なし) -> ulong
    private static ulong? StrToUlong(string s)
    {
        try
        {
            if (s.StartsWith("0x")) return System.Convert.ToUInt64(s[2..], 16);
            if (s.EndsWith('h')) return System.Convert.ToUInt64(s[..^1], 16);
            if (s.StartsWith("0n")) return System.Convert.ToUInt64(s[2..], 10);
            if (s.StartsWith("0t")) return System.Convert.ToUInt64(s[2..], 8);
            if (s.StartsWith("0y")) return System.Convert.ToUInt64(s[2..], 2);
            // MASM が対応していない一般的な基数表現にも対応させる
            if (s.StartsWith("0d")) return System.Convert.ToUInt64(s[2..], 10);
            if (s.StartsWith("0o")) return System.Convert.ToUInt64(s[2..], 8);
            if (s.StartsWith("0b")) return System.Convert.ToUInt64(s[2..], 2);
            // 0 接頭辞は MASM 対応
            if (s.StartsWith('0') && s.Length > 1) return System.Convert.ToUInt64(s[1..], 8);
            // 接頭辞が無い場合は 10 進数として扱う
            return System.Convert.ToUInt64(s, 10);
        }
        catch (Exception)
        {
            // 不正な文字列だった場合
            return null;
        }
    }

    // string -> ulong
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return Binding.DoNothing;
        var s = ((string)value).Trim().ToLower(culture);

        var isMinus = false;

        // 符号処理
        int i;
        for (i = 0; i < s.Length && (s[i] == '-' || s[i] == '+'); i++)
        {
            if (s[i] == '-') isMinus = !isMinus;
        }
        if (i >= s.Length) return Binding.DoNothing;

        var n = StrToUlong(s[i..]);
        if (n == null) return Binding.DoNothing;

        return isMinus ? ~n + 1 : n;
    }
}
