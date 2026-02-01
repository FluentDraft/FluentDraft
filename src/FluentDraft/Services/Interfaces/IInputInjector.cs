using System.Threading.Tasks;

namespace FluentDraft.Services.Interfaces
{
    public interface IInputInjector
    {
        Task TypeTextAsync(string text, bool useClipboard = false);
    }
}
