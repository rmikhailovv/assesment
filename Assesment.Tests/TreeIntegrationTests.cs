using Assesment.Client;
using FluentAssertions;

namespace Assesment.Tests;

public class TreeIntegrationTests : IClassFixture<AssessmentWebApplicationFactory>, IAsyncLifetime
{
    private readonly AssessmentWebApplicationFactory _factory;
    private readonly HttpClient _httpClient;
    private readonly AssessmentApiClient _client;
    private readonly string _testTreeName;

    public TreeIntegrationTests()
    {
        
        _factory = new AssessmentWebApplicationFactory();
        _httpClient = _factory.CreateClient();
        _client = new AssessmentApiClient("http://localhost", _httpClient);
        _testTreeName = $"TestTree_{Guid.NewGuid()}";
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        _httpClient.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetTree_ShouldReturnEmptyTree_WhenTreeDoesNotExist()
    {
        // Act
        var result = await _client.Api_user_tree_getAsync(_testTreeName);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(0);
        result.Name.Should().Be(_testTreeName);
        result.Children.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateNode_ShouldCreateRootNode_WhenParentIdIsNull()
    {
        // Act
        const string nodeName = "RootNode";
        await _client.Api_user_tree_node_createAsync(_testTreeName, null, nodeName);
        var tree = await _client.Api_user_tree_getAsync(_testTreeName);

        // Assert
        tree.Name.Should().Be(nodeName);
        tree.Children.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateNode_ShouldCreateChildNode_WhenParentIdIsProvided()
    {
        // Arrange
        await _client.Api_user_tree_node_createAsync(_testTreeName, null, "RootNode");
        var tree = await _client.Api_user_tree_getAsync(_testTreeName);
        var rootNodeId = tree.Id;

        // Act
        await _client.Api_user_tree_node_createAsync(_testTreeName, rootNodeId, "ChildNode");
        tree = await _client.Api_user_tree_getAsync(_testTreeName);

        // Assert
        tree.Children.Should().HaveCount(1);
        tree.Children.First().Name.Should().Be("ChildNode");
    }

    [Fact]
    public async Task CreateNode_ShouldThrowException_WhenDuplicateNameAmongSiblings()
    {
        // Arrange
        await _client.Api_user_tree_node_createAsync(_testTreeName, null, "RootNode");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
        {
            await _client.Api_user_tree_node_createAsync(_testTreeName, null, "RootNode");
        });

        exception.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task RenameNode_ShouldRenameNode_WhenNewNameIsUnique()
    {
        // Arrange
        await _client.Api_user_tree_node_createAsync(_testTreeName, null, "OldName");
        var tree = await _client.Api_user_tree_getAsync(_testTreeName);
        var nodeId = tree.Id;

        // Act
        await _client.Api_user_tree_node_renameAsync(nodeId, "NewName");
        tree = await _client.Api_user_tree_getAsync(_testTreeName);

        // Assert
        tree.Name.Should().Be("NewName");
    }

    [Fact]
    public async Task DeleteNode_ShouldThrowException_WhenNodeHasChildren()
    {
        // Arrange
        await _client.Api_user_tree_node_createAsync(_testTreeName, null, "RootNode");
        var tree = await _client.Api_user_tree_getAsync(_testTreeName);
        var rootNodeId = tree.Id;
        await _client.Api_user_tree_node_createAsync(_testTreeName, rootNodeId, "ChildNode");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
        {
            await _client.Api_user_tree_node_deleteAsync(rootNodeId);
        });

        exception.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task DeleteNode_ShouldDeleteNode_WhenNodeHasNoChildren()
    {
        // Arrange
        await _client.Api_user_tree_node_createAsync(_testTreeName, null, "RootNode");
        var tree = await _client.Api_user_tree_getAsync(_testTreeName);
        var nodeId = tree.Id;

        // Act
        await _client.Api_user_tree_node_deleteAsync(nodeId);
        tree = await _client.Api_user_tree_getAsync(_testTreeName);

        // Assert
        tree.Children.Should().BeEmpty();
    }
}
