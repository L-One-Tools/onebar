namespace OneBar.Models;

public sealed class WorkflowDefinition
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public required IReadOnlyList<WorkflowStep> Steps { get; init; }
    public IReadOnlyList<WorkflowDefinition> Children { get; init; } = Array.Empty<WorkflowDefinition>();
}

public sealed class WorkflowStep
{
    public required string Name { get; init; }
    public required string Path { get; init; }
}
