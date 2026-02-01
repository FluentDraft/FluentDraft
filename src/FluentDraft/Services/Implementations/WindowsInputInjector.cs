using System.Threading.Tasks;
using WindowsInput;
using FluentDraft.Services.Interfaces;

namespace FluentDraft.Services.Implementations
{
    public class WindowsInputInjector : IInputInjector
    {
        private readonly IInputSimulator _inputSimulator;

        public WindowsInputInjector()
        {
            _inputSimulator = new InputSimulator();
        }

        public async Task TypeTextAsync(string text, bool useClipboard = false)
        {
            if (string.IsNullOrEmpty(text)) return;

            if (useClipboard)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    string oldText = string.Empty;
                    bool hasOldText = false;

                    try
                    {
                        if (System.Windows.Clipboard.ContainsText())
                        {
                            oldText = System.Windows.Clipboard.GetText();
                            hasOldText = true;
                        }

                        System.Windows.Clipboard.SetText(text);
                        await Task.Delay(50); 
                        
                        _inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);

                        await Task.Delay(100);

                        if (hasOldText)
                        {
                            System.Windows.Clipboard.SetText(oldText);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Clipboard injection failed: {ex.Message}");
                        _inputSimulator.Keyboard.TextEntry(text);
                    }
                });
            }
            else
            {
                await Task.Run(() => 
                {
                    try
                    {
                        _inputSimulator.Keyboard.TextEntry(text);
                    }
                    catch (System.Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Input injection failed: {ex.Message}");
                    }
                });
            }
        }
    }
}
