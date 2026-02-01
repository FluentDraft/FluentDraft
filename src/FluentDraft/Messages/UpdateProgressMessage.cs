namespace FluentDraft.Messages
{
    public class UpdateProgressMessage
    {
        public int Progress { get; }

        public UpdateProgressMessage(int progress)
        {
            Progress = progress;
        }
    }
}
