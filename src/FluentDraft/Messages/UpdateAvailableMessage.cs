using Velopack;

namespace FluentDraft.Messages
{
    public class UpdateAvailableMessage
    {
        public UpdateInfo Update { get; }

        public UpdateAvailableMessage(UpdateInfo update)
        {
            Update = update;
        }
    }
}
