namespace DoTrack.Domain.WorkItems;

public sealed class WorkItemHierarchy
{
    public WorkItemId AncestorId { get; private set; }
    public WorkItemId DescendantId { get; private set; }
    public int Depth { get; private set; }

    private WorkItemHierarchy() { }

    public WorkItemHierarchy(WorkItemId ancestor, WorkItemId descendant, int depth)
    {
        AncestorId = ancestor;
        DescendantId = descendant;
        Depth = depth;
    }
}
