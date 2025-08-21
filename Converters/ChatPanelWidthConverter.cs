using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace SharpFM;

public class ChatPanelWidthConverter : IValueConverter
{
    public static readonly ChatPanelWidthConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isVisible)
        {
            return isVisible 
                ? new GridLength(350, GridUnitType.Pixel) 
                : new GridLength(0, GridUnitType.Pixel);
        }
        return new GridLength(0, GridUnitType.Pixel);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}