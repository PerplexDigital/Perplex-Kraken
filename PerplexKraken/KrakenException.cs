using System;

namespace Kraken
{
    [Serializable]
    public class KrakenException : Exception
    {
        public enmStatus Status { get; private set; }
        public KrakenException(enmStatus status) { Status = status; }
    }
}
