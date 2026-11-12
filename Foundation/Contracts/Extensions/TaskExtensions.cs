namespace SecondMonitor.Contracts.Extensions
{
    using System;
    using System.Threading.Tasks;

    using NLog;

    public static class TaskExtensions
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public static async void FireAndForget(this Task task)
        {
            try
            {
                await task;
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "F&F task throwed an exception");
            }
        }
    }
}