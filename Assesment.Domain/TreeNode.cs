namespace Assesment.Domain;

public class TreeNode
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string TreeName { get; set; }
    public long? ParentId { get; set; }
    public TreeNode? Parent { get; set; }
    public List<TreeNode> Children { get; set; } = new();
}
