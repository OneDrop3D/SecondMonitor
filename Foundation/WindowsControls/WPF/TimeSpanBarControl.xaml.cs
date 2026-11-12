namespace SecondMonitor.WindowsControls.WPF
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Animation;

    /// <summary>
    /// Interaction logic for TimeSpanBarControl.xaml
    /// </summary>
    public partial class TimeSpanBarControl : UserControl
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(TimeSpanBarControl), new PropertyMetadata() { DefaultValue = "Title" });
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(TimeSpan), typeof(TimeSpanBarControl), new PropertyMetadata(TimeSpan.Zero, ValuePropertyChangedCallback));
        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(nameof(MaxValue), typeof(TimeSpan), typeof(TimeSpanBarControl), new PropertyMetadata(TimeSpan.FromSeconds(2), ValuePropertyChangedCallback));
        public static readonly DependencyProperty AnimateBarProperty = DependencyProperty.Register(nameof(AnimateBar), typeof(bool), typeof(TimeSpanBarControl));
        public static readonly DependencyProperty HasValidReferenceLapProperty = DependencyProperty.Register(nameof(HasValidReferenceLap), typeof(bool), typeof(TimeSpanBarControl), new PropertyMetadata(false));
        private static readonly TimeSpan _animationSpeed = TimeSpan.FromSeconds(0.3);

        private readonly TranslateTransform _translateTransform;

        public TimeSpanBarControl()
        {
            InitializeComponent();
            _translateTransform = new TranslateTransform(0, 0);
            DeltaRectangle.RenderTransform = _translateTransform;
            UpdateByValue();
        }

        public bool HasValidReferenceLap
        {
            get => (bool)GetValue(HasValidReferenceLapProperty);
            set => SetValue(HasValidReferenceLapProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public TimeSpan Value
        {
            get => (TimeSpan)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public TimeSpan MaxValue
        {
            get => (TimeSpan)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        public bool AnimateBar
        {
            get => (bool)GetValue(AnimateBarProperty);
            set => SetValue(AnimateBarProperty, value);
        }

        private static void ValuePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) => (d as TimeSpanBarControl)?.UpdateByValue();

        private void UpdateByValue()
        {
            double sanitizedValue = Value.TotalSeconds;
            if (sanitizedValue > MaxValue.TotalSeconds)
            {
                sanitizedValue = MaxValue.TotalSeconds;
            }
            else if (sanitizedValue < -MaxValue.TotalSeconds)
            {
                sanitizedValue = -MaxValue.TotalSeconds;
            }

            double maxvaluePortion = sanitizedValue / MaxValue.TotalSeconds;
            if (maxvaluePortion > 0)
            {
                _translateTransform.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, AnimateBar ? _animationSpeed : TimeSpan.Zero));
                //DeltaRectangle.Width = (ActualWidth / 2) * maxvaluePortion;
                DeltaRectangle.BeginAnimation(WidthProperty, new DoubleAnimation((ActualWidth / 2) * maxvaluePortion, AnimateBar ? _animationSpeed : TimeSpan.Zero), HandoffBehavior.SnapshotAndReplace);
            }
            else
            {
                _translateTransform.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation((ActualWidth / 2) * maxvaluePortion, AnimateBar ? _animationSpeed : TimeSpan.Zero));
                DeltaRectangle.BeginAnimation(WidthProperty, new DoubleAnimation((ActualWidth / 2) * -maxvaluePortion, AnimateBar ? _animationSpeed : TimeSpan.Zero), HandoffBehavior.SnapshotAndReplace);
            }
        }
    }
}
