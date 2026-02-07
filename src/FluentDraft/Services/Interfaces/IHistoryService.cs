using System.Collections.Generic;
using System.Threading.Tasks;
using FluentDraft.Models;

namespace FluentDraft.Services.Interfaces
{
    public interface IHistoryService
    {
        Task<List<TranscriptionItem>> GetHistoryAsync();
        Task AddAsync(TranscriptionItem item);
        Task UpdateAsync(TranscriptionItem item);
        Task DeleteAsync(TranscriptionItem item);
        Task ClearAsync();
    }
}
