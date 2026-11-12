namespace SecondMonitor.WindowsControls.WPF.FuelControl
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Animation;

    using DataModel.BasicProperties;

    public class FuelOverviewControl : Control, INotifyPropertyChanged
    {
        public static readonly DependencyProperty FuelPercentageProperty = DependencyProperty.Register(nameof(FuelPercentage), typeof(double), typeof(FuelOverviewControl), new PropertyMetadata() { PropertyChangedCallback = OnFuelLevelChanged });
        public static readonly DependencyProperty BlinkToColorProperty = DependencyProperty.Register(nameof(BlinkToColor), typeof(Color), typeof(FuelOverviewControl), new FrameworkPropertyMetadata { PropertyChangedCallback = OnColorChanged });
        public static readonly DependencyProperty FuelStatusColorProperty = DependencyProperty.Register(nameof(FuelStatusColor), typeof(SolidColorBrush), typeof(FuelOverviewControl));
        public static readonly DependencyProperty FuelStatusUnknownColorProperty = DependencyProperty.Register(nameof(FuelStatusUnknownColor), typeof(Color), typeof(FuelOverviewControl), new FrameworkPropertyMetadata { PropertyChangedCallback = OnColorChanged });
        public static readonly DependencyProperty FuelStatusOkColorProperty = DependencyProperty.Register(nameof(FuelStatusOkColor), typeof(Color), typeof(FuelOverviewControl), new FrameworkPropertyMetadata { PropertyChangedCallback = OnColorChanged });
        public static readonly DependencyProperty FuelStatusNotEnoughColorProperty = DependencyProperty.Register(nameof(FuelStatusNotEnoughColor), typeof(Color), typeof(FuelOverviewControl), new FrameworkPropertyMetadata { PropertyChangedCallback = OnColorChanged });
        public static readonly DependencyProperty FuelStatusMightBeColorProperty = DependencyProperty.Register(nameof(FuelStatusMightBeColor), typeof(Color), typeof(FuelOverviewControl), new FrameworkPropertyMetadata { PropertyChangedCallback = OnColorChanged });
        public static readonly DependencyProperty FuelStateProperty = DependencyProperty.Register(nameof(FuelState), typeof(FuelLevelStatus), typeof(FuelOverviewControl), new PropertyMetadata { PropertyChangedCallback = OnFuelStateChanged });
        public static readonly DependencyProperty MaximumFuelProperty = DependencyProperty.Register(nameof(MaximumFuel), typeof(Volume), typeof(FuelOverviewControl), new PropertyMetadata() { PropertyChangedCallback = OnFuelLevelChanged });

        private Storyboard _toOkStoryBoard;
        private Storyboard _toUnknownStoryBoard;
        private Storyboard _toPossibleEnoughStoryBoard;
        private Storyboard _toNotEnoughStoryBoard;
        private Storyboard _criticalStoryBoard;

        public event PropertyChangedEventHandler PropertyChanged;

        public double FuelPercentage
        {
            get => (double)GetValue(FuelPercentageProperty);
            set => SetValue(FuelPercentageProperty, value);
        }

        public SolidColorBrush FuelStatusColor
        {
            get => (SolidColorBrush)GetValue(FuelStatusColorProperty);
            set => SetValue(FuelStatusColorProperty, value);
        }

        public Color FuelStatusUnknownColor
        {
            get => (Color)GetValue(FuelStatusUnknownColorProperty);
            set => SetValue(FuelStatusUnknownColorProperty, value);
        }

        public FuelLevelStatus FuelState
        {
            get => (FuelLevelStatus)GetValue(FuelStateProperty);
            set => SetValue(FuelStateProperty, value);
        }

        public Color FuelStatusOkColor
        {
            get => (Color)GetValue(FuelStatusOkColorProperty);
            set => SetValue(FuelStatusOkColorProperty, value);
        }

        public Color BlinkToColor
        {
            get => (Color)GetValue(BlinkToColorProperty);
            set => SetValue(BlinkToColorProperty, value);
        }

        public Color FuelStatusNotEnoughColor
        {
            get => (Color)GetValue(FuelStatusNotEnoughColorProperty);
            set => SetValue(FuelStatusNotEnoughColorProperty, value);
        }

        public Color FuelStatusMightBeColor
        {
            get => (Color)GetValue(FuelStatusMightBeColorProperty);
            set => SetValue(FuelStatusMightBeColorProperty, value);
        }

        public Volume MaximumFuel
        {
            get => (Volume)GetValue(MaximumFuelProperty);
            set => SetValue(MaximumFuelProperty, value);
        }

        public Volume FuelLeft
        {
            get;
            private set;
        }

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FuelOverviewControl fuelOverviewControl)
            {
                fuelOverviewControl.UpdateStoryBoards();
            }
        }

        private static void OnFuelStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FuelOverviewControl fuelOverviewControl)
            {
                fuelOverviewControl.StartProperStoryBoard();
            }
        }

        private static void OnFuelLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FuelOverviewControl fuelOverviewControl)
            {
                fuelOverviewControl.UpdateFuelLevels();
            }
        }

        private void StartProperStoryBoard()
        {
            if (_toOkStoryBoard == null || _toNotEnoughStoryBoard == null || _toPossibleEnoughStoryBoard == null
                || _toNotEnoughStoryBoard == null || _criticalStoryBoard == null)
            {
                return;
            }

            /*try
            {
                if (_criticalStoryBoardStarted && _criticalStoryBoard.GetCurrentState(this) == ClockState.Active)
                {
                    _criticalStoryBoard.Stop(this);
                    _criticalStoryBoardStarted = false;
                }
            }
            catch (InvalidOperationException)
            {

            }*/

            switch (FuelState)
            {
                case FuelLevelStatus.Unknown:
                    _toUnknownStoryBoard.Begin(this);
                    break;
                case FuelLevelStatus.IsEnoughForSession:
                    _toOkStoryBoard.Begin(this);
                    break;
                case FuelLevelStatus.PossiblyEnoughForSession:
                    _toPossibleEnoughStoryBoard.Begin(this);
                    break;
                case FuelLevelStatus.NotEnoughForSession:
                    /*_toNotEnoughStoryBoard.Begin(this);
                    break;*/
                case FuelLevelStatus.Critical:
                    _toNotEnoughStoryBoard.Begin(this);
                    /*_criticalStoryBoard.Begin(this);
                    _criticalStoryBoardStarted = true;*/
                    break;
            }
        }

        private void UpdateStoryBoards()
        {
            _toOkStoryBoard = PrepareStoryBoard(FuelStatusOkColor);
            _toUnknownStoryBoard = PrepareStoryBoard(FuelStatusUnknownColor);
            _toPossibleEnoughStoryBoard = PrepareStoryBoard(FuelStatusMightBeColor);
            _toNotEnoughStoryBoard = PrepareStoryBoard(FuelStatusNotEnoughColor);
            _criticalStoryBoard = PrepareCriticalStoryBoard();
        }

        private Storyboard PrepareStoryBoard(Color endColor)
        {
            ColorAnimation toColorAnimation = new ColorAnimation(endColor, TimeSpan.FromSeconds(1));
            Storyboard sb = new Storyboard();
            sb.Children.Add(toColorAnimation);
            Storyboard.SetTarget(toColorAnimation, this);
            Storyboard.SetTargetProperty(toColorAnimation, new PropertyPath("FuelStatusColor.Color"));
            return sb;
        }

        private Storyboard PrepareCriticalStoryBoard()
        {
            ColorAnimation toRedAnimation = new ColorAnimation(FuelStatusNotEnoughColor, BlinkToColor, TimeSpan.FromSeconds(1));
            ColorAnimation backAnimation = new ColorAnimation(BlinkToColor, FuelStatusNotEnoughColor, TimeSpan.FromSeconds(1));
            backAnimation.BeginTime = TimeSpan.FromSeconds(1);
            Storyboard sb = new Storyboard();
            sb.Children.Add(toRedAnimation);
            sb.Children.Add(backAnimation);
            sb.Duration = TimeSpan.FromSeconds(2);
            Storyboard.SetTarget(toRedAnimation, this);
            Storyboard.SetTargetProperty(toRedAnimation, new PropertyPath("FuelStatusColor.Color"));
            Storyboard.SetTarget(backAnimation, this);
            Storyboard.SetTargetProperty(backAnimation, new PropertyPath("FuelStatusColor.Color"));
            sb.RepeatBehavior = RepeatBehavior.Forever;
            return sb;
        }

        private void UpdateFuelLevels()
        {
            if (MaximumFuel == null)
            {
                return;
            }

            Volume newValue = MaximumFuel * FuelPercentage / 100;
            if (FuelLeft != null && Math.Abs(newValue.InLiters - FuelLeft.InLiters) < 0.1)
            {
                return;
            }

            FuelLeft = newValue;
            NotifyPropertyChanged(nameof(FuelLeft));
        }

        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}