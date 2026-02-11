using Assesment.Api.Generated;
using Assesment.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Assesment.Api.Controllers;

public class ApiController : ApiControllerBaseControllerBase
{
    private readonly TreeService _treeService;
    private readonly JournalService _journalService;
    private readonly IConfiguration _configuration;

    public ApiController(
        TreeService treeService, 
        JournalService journalService,
        IConfiguration configuration)
    {
        _treeService = treeService;
        _journalService = journalService;
        _configuration = configuration;
    }

    public override async Task<MRange_MJournalInfo> ApiUserJournalGetRange(
        int skip, 
        int take, 
        object filter, 
        CancellationToken cancellationToken = default)
    {
        DateTime? from = null;
        DateTime? to = null;
        string? search = null;

        // Parse filter object if provided
        if (filter != null)
        {
            var filterDict = filter as IDictionary<string, object>;
            if (filterDict != null)
            {
                if (filterDict.TryGetValue("from", out var fromValue) && fromValue != null)
                {
                    if (DateTime.TryParse(fromValue.ToString(), out var fromDate))
                        from = fromDate;
                }
                if (filterDict.TryGetValue("to", out var toValue) && toValue != null)
                {
                    if (DateTime.TryParse(toValue.ToString(), out var toDate))
                        to = toDate;
                }
                if (filterDict.TryGetValue("search", out var searchValue) && searchValue != null)
                {
                    search = searchValue.ToString();
                }
            }
        }

        var (items, count) = await _journalService.GetRangeAsync(
            skip, take, from, to, search, cancellationToken);

        return new MRange_MJournalInfo
        {
            Skip = skip,
            Count = count,
            Items = items.Select(j => new MJournalInfo
            {
                Id = j.Id,
                EventId = j.EventId,
                CreatedAt = j.CreatedAt.ToString("O")
            }).ToList()
        };
    }

    public override async Task<MJournal> ApiUserJournalGetSingle(
        long id, 
        CancellationToken cancellationToken = default)
    {
        var journal = await _journalService.GetByIdAsync(id, cancellationToken);
        
        if (journal == null)
        {
            throw new KeyNotFoundException($"Journal entry with ID {id} not found.");
        }

        return new MJournal
        {
            Id = journal.Id,
            EventId = journal.EventId,
            CreatedAt = journal.CreatedAt.ToString("O"),
            Text = $"Type: {journal.ExceptionType}\nMessage: {journal.Message}\nEndpoint: {journal.Endpoint}\nQuery: {journal.QueryParameters}\nBody: {journal.BodyParameters}\nStack Trace:\n{journal.StackTrace}"
        };
    }

    public override Task<TokenInfo> ApiUserPartnerRememberMe(
        string code, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(code))
        {
            throw new ArgumentException("Code is required", nameof(code));
        }

        var jwtKey = _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException("JWT configuration is missing");
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(jwtKey);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, code),
                new Claim(ClaimTypes.NameIdentifier, code)
            }),
            Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiryMinutes"] ?? "60")),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return Task.FromResult(new TokenInfo { Token = tokenString });
    }

    public override async Task<MNode> ApiUserTreeGet(
        string treeName, 
        CancellationToken cancellationToken = default)
    {
        var tree = await _treeService.GetOrCreateTreeAsync(treeName, cancellationToken);
        return MapToMNode(tree);
    }

    public override async Task ApiUserTreeNodeCreate(
        string treeName, 
        long? parentNodeId, 
        string nodeName, 
        CancellationToken cancellationToken = default)
    {
        await _treeService.CreateNodeAsync(treeName, parentNodeId, nodeName, cancellationToken);
    }

    public override async Task ApiUserTreeNodeDelete(
        long nodeId, 
        CancellationToken cancellationToken = default)
    {
        await _treeService.DeleteNodeAsync(nodeId, cancellationToken);
    }

    public override async Task ApiUserTreeNodeRename(
        long nodeId, 
        string newNodeName, 
        CancellationToken cancellationToken = default)
    {
        await _treeService.RenameNodeAsync(nodeId, newNodeName, cancellationToken);
    }

    private MNode MapToMNode(Domain.TreeNode node)
    {
        return new MNode
        {
            Id = node.Id,
            Name = node.Name,
            Children = node.Children.Select(MapToMNode).ToList()
        };
    }
}
