using Assesment.Domain;
using Assesment.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Assesment.Infrastructure.Postgres;

public class TreeNodeRepository : ITreeNodeRepository
{
    private readonly AssessmentDbContext _context;

    public TreeNodeRepository(AssessmentDbContext context)
    {
        _context = context;
    }

    public async Task<TreeNode?> GetTreeAsync(string treeName, CancellationToken cancellationToken)
    {
        // Load ALL nodes for this tree in a single query to avoid N+1 problem
        var allNodes = await _context.TreeNodes
            .Where(n => n.TreeName == treeName)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (!allNodes.Any())
        {
            return null;
        }

        // Build tree structure in memory
        var nodeDict = allNodes.ToDictionary(n => n.Id);
        var rootNodes = new List<TreeNode>();

        foreach (var node in allNodes)
        {
            if (node.ParentId == null)
            {
                rootNodes.Add(node);
            }
            else if (nodeDict.TryGetValue(node.ParentId.Value, out var parent))
            {
                parent.Children.Add(node);
            }
        }

        // Create a virtual root node to hold all top-level nodes
        if (rootNodes.Count == 1)
        {
            return rootNodes[0];
        }

        var virtualRoot = new TreeNode
        {
            Id = 0,
            Name = treeName,
            TreeName = treeName,
            Children = rootNodes
        };

        return virtualRoot;
    }

    public async Task<TreeNode?> GetNodeByIdAsync(long nodeId, CancellationToken cancellationToken)
    {
        return await _context.TreeNodes
            .Include(n => n.Parent)
            .Include(n => n.Children)
            .FirstOrDefaultAsync(n => n.Id == nodeId, cancellationToken);
    }

    public async Task<TreeNode> CreateNodeAsync(string treeName, long? parentNodeId, string nodeName, CancellationToken cancellationToken)
    {
        TreeNode? parentNode = null;
        
        if (parentNodeId.HasValue)
        {
            parentNode = await _context.TreeNodes
                .FirstOrDefaultAsync(n => n.Id == parentNodeId.Value, cancellationToken);
            
            if (parentNode == null)
            {
                throw new SecureException($"Parent node with ID {parentNodeId} not found.");
            }

            if (parentNode.TreeName != treeName)
            {
                throw new SecureException($"Parent node does not belong to tree '{treeName}'.");
            }

            // Check for duplicate name among siblings using a targeted query
            var duplicateExists = await _context.TreeNodes
                .AnyAsync(n => n.ParentId == parentNodeId && n.Name == nodeName, cancellationToken);
            
            if (duplicateExists)
            {
                throw new SecureException($"A node with name '{nodeName}' already exists among siblings.");
            }
        }
        else
        {
            // Check for duplicate name among root nodes of the tree
            var existingRoot = await _context.TreeNodes
                .AnyAsync(n => n.TreeName == treeName && n.ParentId == null && n.Name == nodeName, cancellationToken);
            
            if (existingRoot)
            {
                throw new SecureException($"A root node with name '{nodeName}' already exists in tree '{treeName}'.");
            }
        }

        var newNode = new TreeNode
        {
            Name = nodeName,
            TreeName = treeName,
            ParentId = parentNodeId
        };

        _context.TreeNodes.Add(newNode);
        await _context.SaveChangesAsync(cancellationToken);

        return newNode;
    }

    public async Task DeleteNodeAsync(long nodeId, CancellationToken cancellationToken)
    {
        var node = await _context.TreeNodes
            .Include(n => n.Children)
            .FirstOrDefaultAsync(n => n.Id == nodeId, cancellationToken);

        if (node == null)
        {
            throw new SecureException($"Node with ID {nodeId} not found.");
        }

        if (node.Children.Any())
        {
            throw new SecureException("You have to delete all children nodes first.");
        }

        _context.TreeNodes.Remove(node);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RenameNodeAsync(long nodeId, string newNodeName, CancellationToken cancellationToken)
    {
        var node = await _context.TreeNodes
            .FirstOrDefaultAsync(n => n.Id == nodeId, cancellationToken);

        if (node == null)
        {
            throw new SecureException($"Node with ID {nodeId} not found.");
        }

        // Check for duplicate name among siblings using a targeted query
        bool duplicateExists;
        if (node.ParentId.HasValue)
        {
            duplicateExists = await _context.TreeNodes
                .AnyAsync(n => n.ParentId == node.ParentId && n.Id != nodeId && n.Name == newNodeName, cancellationToken);
        }
        else
        {
            duplicateExists = await _context.TreeNodes
                .AnyAsync(n => n.TreeName == node.TreeName && n.ParentId == null && n.Id != nodeId && n.Name == newNodeName, cancellationToken);
        }

        if (duplicateExists)
        {
            throw new SecureException($"A node with name '{newNodeName}' already exists among siblings.");
        }

        node.Name = newNodeName;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
