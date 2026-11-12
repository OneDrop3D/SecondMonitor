namespace SecondMonitor.PCarsConnector
{
    using System;

    public class NameNotFilledException : Exception
    {
        public NameNotFilledException(string msg) : base(msg)
        {
        }
    }
}