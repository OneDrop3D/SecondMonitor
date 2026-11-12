namespace SecondMonitor.Foundation.Connectors
{
    using System;

    public class MessageArgs : EventArgs
    {
        public MessageArgs(string message, Action action)
        {
            this.Action = action;
            this.Message = message;
        }

        public MessageArgs(string message)
            : this(message, null)
        {
        }

        public string Message
        {
            get;
        }

        public bool IsDecision => this.Action != null;

        public Action Action { get; }
    }
}