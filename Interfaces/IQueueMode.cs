using Ongaku.Enums;

namespace Ongaku.Interfaces {
    public interface IQueueMode {
        QueueModeEnum QueueMode { get; set; }
        bool Shuffle { get; set; }
    }
}
