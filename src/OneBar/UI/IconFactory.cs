using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ShapePath = System.Windows.Shapes.Path;

namespace OneBar.UI;

public enum OneBarIcon
{
    LockClosed,
    LockOpen,
    Settings,
    Plus,
    Refresh,
    Close,
    Back
}

public static class IconFactory
{
    private static readonly Brush Stroke = new SolidColorBrush(Color.FromRgb(93, 100, 105));
    private const double ThinStroke = 1.15;

    public static FrameworkElement Create(OneBarIcon icon)
    {
        var size = icon == OneBarIcon.Settings ? 21.5 : icon is OneBarIcon.LockClosed or OneBarIcon.LockOpen ? 20.5 : 19.5;
        var viewbox = new Viewbox
        {
            Width = size,
            Height = size,
            Stretch = Stretch.Uniform,
            IsHitTestVisible = false
        };

        viewbox.Child = icon switch
        {
            OneBarIcon.Settings => Gear(),
            OneBarIcon.LockClosed => Lock(closed: true),
            OneBarIcon.LockOpen => Lock(closed: false),
            OneBarIcon.Plus => SimplePath("M12,4 L12,20 M4,12 L20,12"),
            OneBarIcon.Refresh => SimplePath("M19,8 A8,8 0 1 0 20,14 M19,8 L19,3 M19,8 L14,8"),
            OneBarIcon.Close => SimplePath("M5,5 L19,19 M19,5 L5,19"),
            OneBarIcon.Back => SimplePath("M15,4 L7,12 L15,20"),
            _ => new Canvas { Width = 24, Height = 24 }
        };
        return viewbox;
    }

    private static Canvas Gear()
    {
        var canvas = BaseCanvas();
        for (var i = 0; i < 8; i++)
        {
            var layer = new Canvas
            {
                Width = 24,
                Height = 24,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform(i * 45)
            };
            var tooth = new Rectangle
            {
                Width = 3.2,
                Height = 5.2,
                RadiusX = 0.8,
                RadiusY = 0.8,
                Fill = Brushes.Transparent,
                Stroke = Stroke,
                StrokeThickness = ThinStroke
            };
            Canvas.SetLeft(tooth, 10.4);
            Canvas.SetTop(tooth, 0.8);
            layer.Children.Add(tooth);
            canvas.Children.Add(layer);
        }

        var outer = new Ellipse
        {
            Width = 14.2,
            Height = 14.2,
            Stroke = Stroke,
            StrokeThickness = ThinStroke,
            Fill = Brushes.Transparent
        };
        Canvas.SetLeft(outer, 4.9);
        Canvas.SetTop(outer, 4.9);
        canvas.Children.Add(outer);

        var inner = new Ellipse
        {
            Width = 5.8,
            Height = 5.8,
            Stroke = Stroke,
            StrokeThickness = ThinStroke,
            Fill = Brushes.Transparent
        };
        Canvas.SetLeft(inner, 9.1);
        Canvas.SetTop(inner, 9.1);
        canvas.Children.Add(inner);
        return canvas;
    }

    private static Canvas Lock(bool closed)
    {
        var canvas = BaseCanvas();
        var body = new Border
        {
            Width = 14,
            Height = 10,
            BorderBrush = Stroke,
            BorderThickness = new Thickness(ThinStroke),
            CornerRadius = new CornerRadius(2.2),
            Background = Brushes.Transparent
        };
        Canvas.SetLeft(body, 5);
        Canvas.SetTop(body, 11);
        canvas.Children.Add(body);

        var shackle = new ShapePath
        {
            Stroke = Stroke,
            StrokeThickness = ThinStroke,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Fill = Brushes.Transparent,
            Data = Geometry.Parse(closed
                ? "M8,11 L8,8 C8,3.8 16,3.8 16,8 L16,11"
                : "M8,11 L8,8 C8,4 15.5,4 15.5,8")
        };
        canvas.Children.Add(shackle);

        var key = new Ellipse { Width = 2.1, Height = 2.1, Fill = Stroke };
        Canvas.SetLeft(key, 10.95);
        Canvas.SetTop(key, 14.6);
        canvas.Children.Add(key);
        return canvas;
    }

    private static Canvas SimplePath(string data)
    {
        var canvas = BaseCanvas();
        canvas.Children.Add(new ShapePath
        {
            Stroke = Stroke,
            StrokeThickness = ThinStroke,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            Fill = Brushes.Transparent,
            Data = Geometry.Parse(data)
        });
        return canvas;
    }

    private static Canvas BaseCanvas() => new() { Width = 24, Height = 24 };
}
