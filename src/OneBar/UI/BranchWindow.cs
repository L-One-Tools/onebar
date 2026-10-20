using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OneBar.Models;
namespace OneBar.UI;

// Separate transparent HWND keeps the branches outside the toolbar backplate.
public sealed class BranchWindow : Window
{
    private readonly MainWindow _main;
    private readonly Canvas _canvas = new() { VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left };
    private readonly ScrollViewer _scroll;
    private readonly List<(Button button,string path,int depth)> _files = new();
    private readonly List<List<FrameworkElement>> _levels = new();
    private string? _selectedFile, _category;
    internal int LevelCount => _levels.Count;
    public BranchWindow(MainWindow main)
    {
        _main = main; Owner = main; Title = "OneBar 悬浮分支";
        WindowStyle = WindowStyle.None; AllowsTransparency = true; Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize; Topmost = true; ShowInTaskbar = false; ShowActivated = false;
        _scroll = new ScrollViewer { Content = _canvas,VerticalContentAlignment = VerticalAlignment.Top,HorizontalContentAlignment = HorizontalAlignment.Left,VerticalScrollBarVisibility = ScrollBarVisibility.Auto,HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,CanContentScroll = false };
        Content = _scroll; MouseEnter += (_,_) => _main.CancelCollapse(); MouseLeave += (_,_) => _main.ScheduleCollapse();
        PreviewKeyDown += (_,e) => { if (e.Key == System.Windows.Input.Key.Escape) { Hide(); _main.Focus(); e.Handled = true; } };
    }
    public void Render(IReadOnlyList<WorkflowDefinition> path,double anchorX,double top)
    {
        if (_category != path[0].Name) _selectedFile=null;
        _category=path[0].Name; _files.Clear(); _levels.Clear();
        var work = SystemParameters.WorkArea; Left = work.Left + 16; Top = top; Width = work.Width - 32; Height = Math.Max(100,work.Bottom - top - 20);
        _canvas.Children.Clear(); _canvas.Width = Width - 18; var available = _canvas.Width;
        const double capsuleHeight = 32, gap = 6;
        double parentX = Math.Clamp(anchorX - Left,30,available - 30),y = 2,selectedTop = 0;
        for (int depth = 0; depth < path.Count; depth++)
        {
            var levelElements = new List<FrameworkElement>();
            _levels.Add(levelElements);
            var folder = path[depth];
            var nodes = folder.Children.Select(x => (label:x.Name,folder:(WorkflowDefinition?)x,file:(WorkflowStep?)null))
                .Concat(folder.Steps.Select(x => (label:x.Name,folder:(WorkflowDefinition?)null,file:(WorkflowStep?)x))).ToList();
            if (nodes.Count == 0)
            {
                var empty = new TextBlock { Text = "这个文件夹还没有提示词",Foreground = MainWindow.Brush("#535B63"),Background = Brushes.White,Padding = new Thickness(12,8,12,8) };
                Canvas.SetLeft(empty,Math.Clamp(parentX - 90,8,available - 200)); Canvas.SetTop(empty,y); _canvas.Children.Add(empty); levelElements.Add(empty); y += 60; break;
            }
            var width = Math.Clamp(nodes.Max(n => n.label.Length) * 11 + 28,104,210);
            int cols = Math.Max(1,Math.Min(nodes.Count,(int)((available - 24) / (width + gap))));
            int rows = (int)Math.Ceiling(nodes.Count / (double)cols);
            double rowWidth = cols * (width + gap) - gap,startX = Math.Clamp(parentX - rowWidth / 2,12,Math.Max(12,available - rowWidth - 12));
            double nextX = parentX;
            for (int i = 0; i < nodes.Count; i++)
            {
                var item = nodes[i]; var level = depth;
                double x = startX + (i % cols) * (width + gap),itemY = y + (i / cols) * (capsuleHeight + gap);
                bool selected = item.folder != null && depth + 1 < path.Count && path[depth + 1].Path == item.folder.Path;
                var b = _main.Capsule(item.label,width,"CatalogButtonStyle",
                    item.folder != null ? "点击展开 / 收起 · " + item.folder.Path : "单击复制全文；按住拖动文件 · " + item.file!.Path);
                b.Height = capsuleHeight;
                BranchPalette.Apply(b,_category,depth,selected || (item.file != null && item.file.Path == _selectedFile));
                if (item.folder != null)
                {
                    var content = new StackPanel { Orientation = Orientation.Horizontal };
                    content.Children.Add(new TextBlock { Text = item.label,FontSize = 11,FontWeight = FontWeights.SemiBold,MaxWidth = width - 54,TextTrimming = TextTrimming.CharacterEllipsis });
                    content.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse(selected ? "M 0,4 L 4,0 L 8,4" : "M 0,0 L 4,4 L 8,0"),Stroke = b.Foreground,StrokeThickness = 1.5,Width = 8,Height = 5,Margin = new Thickness(12,5,0,0) });
                    b.Content = content; b.Click += (_,_) => _main.SelectFolder(level,item.folder);
                }
                else
                {
                    var file=item.file!; _files.Add((b,file.Path,depth));
                    _main.AttachFile(b,file,() =>
                    {
                        _selectedFile=file.Path;
                        foreach (var entry in _files) BranchPalette.Apply(entry.button,_category,entry.depth,entry.path == _selectedFile);
                    });
                }
                Canvas.SetLeft(b,x); Canvas.SetTop(b,itemY); _canvas.Children.Add(b);
                levelElements.Add(b);
                if (selected) nextX = x + width / 2;
            }
            parentX = nextX; selectedTop = y; y += rows * (capsuleHeight + gap);
        }
        _canvas.Height = y + 6;
        if (selectedTop + 80 > Height) _scroll.ScrollToVerticalOffset(selectedTop + 100 - Height);
    }

    // One stage per depth, deepest first. The final stage belongs to the main bar.
    internal void ApplyRetraction(double stages)
    {
        for (int depth = _levels.Count - 1; depth >= 0; depth--)
        {
            var progress = Math.Clamp(stages - (_levels.Count - 1 - depth), 0, 1);
            var eased = 1 - Math.Pow(1 - progress, 3);
            foreach (var element in _levels[depth])
            {
                element.Opacity = 1 - eased;
                element.RenderTransform = new TranslateTransform(0, -6 * eased);
                element.Visibility = progress >= 1 ? Visibility.Hidden : Visibility.Visible;
            }
        }
        if (stages >= _levels.Count) Hide();
    }
}
