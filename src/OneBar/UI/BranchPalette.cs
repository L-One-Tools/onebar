using System.Windows.Controls;
using System.Windows.Media;
namespace OneBar.UI;

public static class BranchPalette
{
    // Five stops follow the reference: darkest first, progressively lighter.
    private static readonly Dictionary<string,string[]> Families = new()
    {
        ["blue"] = new[] { "#153F70", "#1D5792", "#2269AD", "#65A9E1", "#83BDEB" },
        ["aqua"] = new[] { "#25545E", "#2E6C78", "#358A98", "#49B2BD", "#99D5DA" },
        ["green"] = new[] { "#415B29", "#5D8134", "#739C40", "#96CB4B", "#C8E6A9" },
        ["purple"] = new[] { "#333456", "#484B75", "#585C8D", "#7C80AD", "#ADB2D3" },
        ["pink"] = new[] { "#62334F", "#804064", "#A34C7E", "#DB60A0", "#EDABD0" },
        ["red"] = new[] { "#5E232A", "#7E3038", "#9B3542", "#C7424C", "#E2A1AB" },
        ["orange"] = new[] { "#704134", "#945442", "#B5644E", "#EE805F", "#F8BDAF" },
        ["yellow"] = new[] { "#685933", "#907B40", "#B29648", "#EDBC58", "#F7DDA4" },
        ["indigo"] = new[] { "#303D71", "#45558C", "#5D70A8", "#8E9DCA", "#C0C9E6" }
    };
    private static readonly Dictionary<string,string> Categories = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AI博主实验"]="pink", ["Image Lib"]="blue", ["One小报"]="orange",
        ["Style Lib"]="aqua", ["产品调研"]="green", ["修图"]="purple",
        ["分析整理"]="red", ["常用指令"]="yellow", ["风格化"]="indigo"
    };
    public static string FamilyFor(string name)
    {
        if (Categories.TryGetValue(name,out var family)) return family;
        // Stable across processes and directory reordering.
        uint hash=2166136261; foreach (char c in name) hash=unchecked((hash ^ c)*16777619);
        return Families.Keys.ElementAt((int)(hash % (uint)Families.Count));
    }
    public static Color Tone(string category,int depth) => (Color)ColorConverter.ConvertFromString(Families[FamilyFor(category)][Math.Clamp(depth,0,4)]);
    public static double Luminance(Color c)
    {
        static double Channel(byte v) { double s=v/255.0; return s<=0.04045 ? s/12.92 : Math.Pow((s+0.055)/1.055,2.4); }
        return 0.2126*Channel(c.R)+0.7152*Channel(c.G)+0.0722*Channel(c.B);
    }
    public static void Apply(Button button,string category,int depth,bool selected)
    {
        button.ClearValue(Control.BackgroundProperty); button.ClearValue(Control.ForegroundProperty); button.ClearValue(Control.BorderBrushProperty);
        if (!selected) return;
        var color=Tone(category,depth);
        // Pick the readable foreground, without introducing a grey branch palette.
        var dark=(Color)ColorConverter.ConvertFromString("#18212B");
        double whiteContrast=1.05/(Luminance(color)+0.05), darkContrast=(Luminance(color)+0.05)/(Luminance(dark)+0.05);
        if (whiteContrast < 4.5 && darkContrast < 4.5) { dark=Colors.Black; darkContrast=(Luminance(color)+0.05)/0.05; }
        button.Background=new SolidColorBrush(color);
        button.Foreground=new SolidColorBrush(whiteContrast >= darkContrast ? Colors.White : dark);
        button.BorderBrush=new SolidColorBrush(Color.FromArgb(160,255,255,255));
    }
}
