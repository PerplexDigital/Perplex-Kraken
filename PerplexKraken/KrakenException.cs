using System;

namespace Kraken
{
    [Serializable]
    public class KrakenException : Exception
    {
        public enmStatus Status { get; private set; }
        public KrakenException(enmStatus status) : base("Calling the Kraken API resulted in an HTTP exception: (" + status.ToString("d") + ") " + Helper.GetEnumDescription(status)) { Status = status; }
    }
}
