using System.Text.RegularExpressions;
using OneBar.Models;

namespace OneBar.Services;

public sealed class WorkflowService
{
    private static readonly Regex LeadingIndexKeep = new(@"^\s*(\d+)\s*[-_. ]*\s*", RegexOptions.Compiled);
    private static readonly Regex LeadingIndexRemove = new(@"^\s*\d+\s*[-_. ]*\s*", RegexOptions.Compiled);

    public IReadOnlyList<WorkflowDefinition> Load(string root)
    {
        try
        {
            Directory.CreateDirectory(root);
            return Directory.GetDirectories(root)
                .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
                .Where(dir => (File.GetAttributes(dir) & FileAttributes.ReparsePoint) == 0)
                .Select(ReadDirectory)
                .ToList();
        }
        catch
        {
            return Array.Empty<WorkflowDefinition>();
        }
    }

    private WorkflowDefinition ReadDirectory(string dir) => new()
    {
        Name = DisplayName(Path.GetFileName(dir), false), Path = dir,
        Children = Load(dir),
        Steps = Directory.GetFiles(dir)
            .Where(file => Path.GetExtension(file).Equals(".md", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(file).Equals(".txt", StringComparison.OrdinalIgnoreCase))
            .OrderBy(file => Path.GetFileName(file), StringComparer.OrdinalIgnoreCase)
            .Select(file => new WorkflowStep { Name = DisplayName(Path.GetFileName(file), true), Path = file }).ToList()
    };

    public static string DisplayName(string name, bool keepIndex)
    {
        var value = Path.GetFileNameWithoutExtension(name).Replace('_', ' ');
        value = keepIndex
            ? LeadingIndexKeep.Replace(value, "$1 ")
            : LeadingIndexRemove.Replace(value, string.Empty);
        return value.Trim();
    }
}
