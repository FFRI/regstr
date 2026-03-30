/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Documents;
using DiffPlex.Model;
using Brushes = System.Windows.Media.Brushes;

namespace CommandViewer;

// diff タイプ
public enum DiffChunkerKind
{
    // diff 無し
    None,
    // 文字単位
    Char,
    // (半角空白区切り) 単語単位
    Word,
    // 行単位
    Line
}

public class DiffInfo(DiffResult res, DiffChunkerKind kind)
{
    public DiffResult Res = res;
    public DiffChunkerKind Kind = kind;
}

// TextBlock版
public class DiffValueConverter : IValueConverter
{
    private static List<Inline> MakeDiffInlines(DiffResult diff)
    {
        var inlines = new List<Inline>();

        var posB = 0; // 新しい方の pos
        var s = new StringBuilder(1024);
        if (diff.DiffBlocks.Count == 0)
        {
            // 差分が無い場合
            foreach (var ch in diff.PiecesNew) s.Append(ch);
            inlines.Add(new Run(s.ToString()));

            return inlines;
        }
        foreach (var block in diff.DiffBlocks)
        {
            // 通常の文章
            s.Clear();
            for (; posB < block.InsertStartB; posB++) s.Append(diff.PiecesNew[posB]);

            inlines.Add(new Run(s.ToString()));

            // 差分を赤文字にする
            s.Clear();
            var stop = posB + block.InsertCountB;
            for (; posB < stop; posB++) s.Append(diff.PiecesNew[posB]);

            inlines.Add(new Run(s.ToString())
            {
                Foreground = Brushes.Red
            });
        }

        // 末尾
        s.Clear();
        for (; posB < diff.PiecesNew.Count; posB++) s.Append(diff.PiecesNew[posB]);
        inlines.Add(new Run(s.ToString()));

        return inlines;
    }

    private static List<Inline> MakeDiffInlinesLine(DiffResult diff)
    {
        var inlines = new List<Inline>();

        var posB = 0; // 新しい方の pos
        if (diff.DiffBlocks.Count == 0)
        {
            // 差分が無い場合
            foreach (var line in diff.PiecesNew) inlines.Add(new Run(line + "\n"));

            return inlines;
        }
        foreach (var block in diff.DiffBlocks)
        {
            // 通常の文章
            for (; posB < block.InsertStartB; posB++) inlines.Add(new Run(diff.PiecesNew[posB] + "\n"));

            // 差分を赤文字にする
            var stop = posB + block.InsertCountB;
            for (; posB < stop; posB++)
            {
                inlines.Add(new Run(diff.PiecesNew[posB] + "\n")
                {
                    Foreground = Brushes.Red
                }
                );
            }
        }

        // 末尾
        for (; posB < diff.PiecesNew.Count; posB++) inlines.Add(new Run(diff.PiecesNew[posB] + "\n"));

        return inlines;
    }

    // DiffResult -> RichTextBox (Diff)
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DiffInfo diff) return Binding.DoNothing;

        switch (diff.Kind)
        {
            case DiffChunkerKind.None:
                if (diff.Res.PiecesNew.Count == 0) return null;
                return new List<Inline>([new Run(diff.Res.PiecesNew[0])]);
            case DiffChunkerKind.Char:
            case DiffChunkerKind.Word:
                return MakeDiffInlines(diff.Res);
            case DiffChunkerKind.Line:
                return MakeDiffInlinesLine(diff.Res);
            default:
                throw new UnreachableException();
        }
    }

    // 使用しない
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new UnreachableException();
    }
}

// TextBox版
public class DiffTextBoxValueConverter : IValueConverter
{

    private static string MakeDiffInlines(DiffResult diff)
    {
        StringBuilder s = new(1024);
        foreach (var x in diff.PiecesNew) s.Append(x);
        return s.ToString();
    }

    private static string MakeDiffInlinesLine(DiffResult diff)
    {
        StringBuilder s = new(1024);
        foreach (var x in diff.PiecesNew)
        {
            s.Append(x);
            s.Append("\n");
        }
        return s.ToString();
    }

    // DiffResult -> string
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DiffInfo diff) return Binding.DoNothing;

        switch (diff.Kind)
        {
            case DiffChunkerKind.None:
                if (diff.Res.PiecesNew.Count == 0) return null;
                return MakeDiffInlines(diff.Res);
            case DiffChunkerKind.Char:
            case DiffChunkerKind.Word:
                return MakeDiffInlines(diff.Res);
            case DiffChunkerKind.Line:
                return MakeDiffInlinesLine(diff.Res);
            default:
                throw new UnreachableException();
        }
    }

    // 使用しない
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new UnreachableException();
    }
}
