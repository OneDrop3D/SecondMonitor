namespace SecondMonitor.ViewModels.StatusIcon
{
    using System;
    using System.Timers;

    using SecondMonitor.DataModel.BasicProperties;

    public class DamageStatusIconViewModel : StatusIconViewModel, IDisposable
    {
        private readonly Timer _clearBorderTimer;
        private double _oldDamage;

        public DamageStatusIconViewModel()
        {
            _clearBorderTimer = new Timer(TimeSpan.FromSeconds(5));
            _clearBorderTimer.AutoReset = false;
            _clearBorderTimer.Elapsed += ClearBorderTimerOnElapsed;
        }

        public void ApplyDamage(DamageInformation damageInformation)
        {
            if (damageInformation.Damage < damageInformation.MediumDamageThreshold)
            {
                IconState = StatusIconState.Unlit;
            }
            else if (damageInformation.Damage < damageInformation.HeavyDamageThreshold)
            {
                IconState = StatusIconState.Warning;
            }
            else
            {
                IconState = StatusIconState.Error;
            }

            double damage = Math.Round(Math.Clamp(damageInformation.Damage, 0, 1) * 100);
            if (_oldDamage == damage)
            {
                return;
            }

            BorderState = damage > _oldDamage ? StatusIconState.Warning : StatusIconState.Ok;
            _oldDamage = damage;
            _clearBorderTimer.Stop();
            _clearBorderTimer.Start();

            AdditionalText = damage > 0 ? damage.ToString("F0") : string.Empty;
        }

        private void ClearBorderTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            BorderState = StatusIconState.Unlit;
        }

        public void Dispose()
        {
            _clearBorderTimer?.Dispose();
        }
    }
}