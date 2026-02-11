using Assesment.Client;
using FluentAssertions;

namespace Assesment.Tests;

public class JournalIntegrationTests : IClassFixture<AssessmentWebApplicationFactory>, IAsyncLifetime
{
    private readonly AssessmentWebApplicationFactory _factory;
    private readonly HttpClient _httpClient;
    private readonly AssessmentApiClient _client;

    public JournalIntegrationTests(AssessmentWebApplicationFactory factory)
    {
        _factory = factory;
        _httpClient = _factory.CreateClient();
        _client = new AssessmentApiClient("http://localhost", _httpClient);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        _httpClient.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ExceptionJournal_ShouldLogSecureException_WhenSecureExceptionIsThrown()
    {
        // Arrange - create a duplicate node to trigger SecureException
        var treeName = $"ExceptionTest_{Guid.NewGuid()}";
        await _client.Api_user_tree_node_createAsync(treeName, null, "TestNode");

        // Act - try to create duplicate
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
        {
            await _client.Api_user_tree_node_createAsync(treeName, null, "TestNode");
        });

        // Assert
        exception.StatusCode.Should().Be(500);
        
        // Check journal
        var journals = await _client.Api_user_journal_getRangeAsync(0, 10, null);
        journals.Items.Should().NotBeEmpty();
        journals.Items.Should().Contain(j => j.EventId > 0);
    }

    [Fact]
    public async Task GetJournalRange_ShouldReturnPaginatedResults()
    {
        // Act
        var result = await _client.Api_user_journal_getRangeAsync(0, 5, null);

        // Assert
        result.Should().NotBeNull();
        result.Skip.Should().Be(0);
        result.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetJournalSingle_ShouldReturnJournalDetails_WhenIdExists()
    {
        // Arrange - trigger an exception first
        var treeName = $"ExceptionTest_{Guid.NewGuid()}";
        await _client.Api_user_tree_node_createAsync(treeName, null, "TestNode");
        
        try
        {
            await _client.Api_user_tree_node_createAsync(treeName, null, "TestNode");
        }
        catch (ApiException)
        {
            // Expected exception
        }

        // Get the latest journal entry
        var journals = await _client.Api_user_journal_getRangeAsync(0, 1, null);
        if (journals.Items.Any())
        {
            var journalId = journals.Items.First().Id;

            // Act
            var journal = await _client.Api_user_journal_getSingleAsync(journalId);

            // Assert
            journal.Should().NotBeNull();
            journal.Id.Should().Be(journalId);
            journal.Text.Should().NotBeNullOrEmpty();
        }
    }
}
