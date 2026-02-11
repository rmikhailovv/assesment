using Assesment.Domain;

namespace Assesment.Infrastructure;

public interface IExceptionJournalRepository
{
    Task<long> CreateAsync(ExceptionJournal journal, CancellationToken cancellationToken);
    Task<ExceptionJournal?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<(List<ExceptionJournal> Items, int Count)> GetRangeAsync(int skip, int take, DateTime? from, DateTime? to, string? search, CancellationToken cancellationToken);
}
