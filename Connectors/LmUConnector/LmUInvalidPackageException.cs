namespace SecondMonitor.LmUConnector
{
    using System;

    public class LMUInvalidPackageException : Exception
    {
        public LMUInvalidPackageException(Exception innerException)
            : base(string.Empty, innerException)
        {
        }

        public LMUInvalidPackageException(string msg)
            : base(msg)
        {
        }
    }
}