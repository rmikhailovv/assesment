using Assesment.Domain;
using Assesment.Infrastructure;

namespace Assesment.Application;

public class JournalService
{
    private readonly IExceptionJournalRepository _journalRepository;

    public JournalService(IExceptionJournalRepository journalRepository)
    {
        _journalRepository = journalRepository;
    }

    public async Task<ExceptionJournal?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return await _journalRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<(List<ExceptionJournal> Items, int Count)> GetRangeAsync(
        int skip, 
        int take, 
        DateTime? from, 
        DateTime? to, 
        string? search, 
        CancellationToken cancellationToken)
    {
        return await _journalRepository.GetRangeAsync(skip, take, from, to, search, cancellationToken);
    }
}
