using Assesment.Domain;
using Assesment.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Assesment.Infrastructure.Postgres;

public class ExceptionJournalRepository : IExceptionJournalRepository
{
    private readonly AssessmentDbContext _context;

    public ExceptionJournalRepository(AssessmentDbContext context)
    {
        _context = context;
    }

    public async Task<long> CreateAsync(ExceptionJournal journal, CancellationToken cancellationToken)
    {
        journal.CreatedAt = DateTime.UtcNow;
        journal.EventId = DateTime.UtcNow.Ticks;
        
        _context.ExceptionJournal.Add(journal);
        await _context.SaveChangesAsync(cancellationToken);
        
        return journal.EventId;
    }

    public async Task<ExceptionJournal?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return await _context.ExceptionJournal
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<(List<ExceptionJournal> Items, int Count)> GetRangeAsync(
        int skip, 
        int take, 
        DateTime? from, 
        DateTime? to, 
        string? search, 
        CancellationToken cancellationToken)
    {
        var query = _context.ExceptionJournal.AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(j => j.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(j => j.CreatedAt <= to.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(j => 
                j.Message.Contains(search) || 
                j.ExceptionType.Contains(search) ||
                j.StackTrace.Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
