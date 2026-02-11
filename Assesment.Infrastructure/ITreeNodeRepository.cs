using Assesment.Domain;

namespace Assesment.Infrastructure;

public interface ITreeNodeRepository
{
    Task<TreeNode?> GetTreeAsync(string treeName, CancellationToken cancellationToken);
    Task<TreeNode?> GetNodeByIdAsync(long nodeId, CancellationToken cancellationToken);
    Task<TreeNode> CreateNodeAsync(string treeName, long? parentNodeId, string nodeName, CancellationToken cancellationToken);
    Task DeleteNodeAsync(long nodeId, CancellationToken cancellationToken);
    Task RenameNodeAsync(long nodeId, string newNodeName, CancellationToken cancellationToken);
}