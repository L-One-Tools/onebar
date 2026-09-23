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
                .Select(dir => new WorkflowDefinition
                {
                    Name = DisplayName(Path.GetFileName(dir), keepIndex: false),
                    Path = dir,
                    Steps = Directory.GetFiles(dir)
                        .Where(file => string.Equals(Path.GetExtension(file), ".md", StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(Path.GetExtension(file), ".txt", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
                        .Select(file => new WorkflowStep
                        {
                            Name = DisplayName(Path.GetFileName(file), keepIndex: true),
                            Path = file
                        })
                        .ToList()
                })
                .ToList();
        }
        catch
        {
            return Array.Empty<WorkflowDefinition>();
        }
    }

    public static string DisplayName(string name, bool keepIndex)
    {
        var value = Path.GetFileNameWithoutExtension(name).Replace('_', ' ');
        value = keepIndex
            ? LeadingIndexKeep.Replace(value, "$1 ")
            : LeadingIndexRemove.Replace(value, string.Empty);
        return value.Trim();
    }
}
