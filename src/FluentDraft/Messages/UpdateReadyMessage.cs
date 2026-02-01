using Velopack;

namespace FluentDraft.Messages
{
    public class UpdateReadyMessage
    {
        public UpdateInfo Update { get; }

        public UpdateReadyMessage(UpdateInfo update)
        {
            Update = update;
        }
    }
}
