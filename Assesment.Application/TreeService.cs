using Assesment.Domain;
using Assesment.Infrastructure;

namespace Assesment.Application;

public class TreeService
{
    private readonly ITreeNodeRepository _treeNodeRepository;

    public TreeService(ITreeNodeRepository treeNodeRepository)
    {
        _treeNodeRepository = treeNodeRepository;
    }

    public async Task<TreeNode> GetOrCreateTreeAsync(string treeName, CancellationToken cancellationToken)
    {
        var tree = await _treeNodeRepository.GetTreeAsync(treeName, cancellationToken);
        
        if (tree == null)
        {
            // Create a virtual root for an empty tree
            tree = new TreeNode
            {
                Id = 0,
                Name = treeName,
                TreeName = treeName,
                Children = new List<TreeNode>()
            };
        }

        return tree;
    }

    public async Task<TreeNode> CreateNodeAsync(string treeName, long? parentNodeId, string nodeName, CancellationToken cancellationToken)
    {
        return await _treeNodeRepository.CreateNodeAsync(treeName, parentNodeId, nodeName, cancellationToken);
    }

    public async Task DeleteNodeAsync(long nodeId, CancellationToken cancellationToken)
    {
        await _treeNodeRepository.DeleteNodeAsync(nodeId, cancellationToken);
    }

    public async Task RenameNodeAsync(long nodeId, string newNodeName, CancellationToken cancellationToken)
    {
        await _treeNodeRepository.RenameNodeAsync(nodeId, newNodeName, cancellationToken);
    }
}
