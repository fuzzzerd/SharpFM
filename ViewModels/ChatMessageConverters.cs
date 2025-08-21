using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace SharpFM.ViewModels;

public class ChatMessageAlignmentConverter : IValueConverter
{
    public static readonly ChatMessageAlignmentConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isUser)
        {
            return isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        }
        return HorizontalAlignment.Left;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ChatMessageBrushConverter : IValueConverter
{
    public static readonly ChatMessageBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isUser)
        {
            if (isUser)
            {
                // User messages - use accent color
                return new SolidColorBrush(Color.FromRgb(0, 120, 212));
            }
            else
            {
                // Assistant messages - use theme-aware background
                return new SolidColorBrush(Color.FromArgb(255, 64, 64, 64)); // Dark gray for better contrast
            }
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ChatMessageTextBrushConverter : IValueConverter
{
    public static readonly ChatMessageTextBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isUser)
        {
            // Both user and assistant messages should have white text for better contrast
            return new SolidColorBrush(Colors.White);
        }
        return new SolidColorBrush(Colors.White);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ClipContextConverter : IValueConverter
{
    public static readonly ClipContextConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool hasSelectedClip)
        {
            return hasSelectedClip ? "Context: Selected clip loaded" : "No clip selected";
        }
        return "No clip selected";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}