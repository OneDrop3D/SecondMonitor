namespace SecondMonitor.Remote.Models
{
    using System;
    using LiteNetLib;
    using SecondMonitor.ViewModels.PluginsSettings;

    public class LiteNetLibHostAddressValidator : IHostAddressValidator
    {
        public bool IsValidHostAddress(string value)
        {
            try
            {
                NetUtils.ResolveAddress(value);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}