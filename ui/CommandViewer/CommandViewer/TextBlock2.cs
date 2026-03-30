/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace CommandViewer;

// Inlines をバインディングできるようにした TextBlock
public class TextBlock2 : TextBlock
{
    public IEnumerable<Inline>? Inlines2
    {
        get => (IEnumerable<Inline>?)GetValue(Inlines2Property);
        set => SetValue(Inlines2Property, value);
    }

    public static readonly DependencyProperty Inlines2Property =
        DependencyProperty.Register(
            nameof(Inlines2),
            typeof(IEnumerable<Inline>),
            typeof(TextBlock2),
            new PropertyMetadata(null, OnInlines2Changed));

    private static void OnInlines2Changed(DependencyObject dp, DependencyPropertyChangedEventArgs e)
    {
        var textBlock = (TextBlock2)dp;
        textBlock.Inlines.Clear();

        if (e.NewValue is not IEnumerable<Inline> inlines) return;

        foreach (var inline in inlines)
        {
            textBlock.Inlines.Add(inline);
        }
    }
}
