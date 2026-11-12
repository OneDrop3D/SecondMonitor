namespace SecondMonitor.Timing.Application.Presentation.Controls.TimingGridControl
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;
    using Application.TimingGrid;
    using SessionTiming.Drivers.Presentation.ViewModel;
    using WindowsControls.WPF.DataGrid;

    public class TimingGrid : DynamicGrid
    {
        public static readonly DependencyProperty HighlightPlayerProperty = DependencyProperty.Register("HighlightPlayer", typeof(bool), typeof(TimingGrid), new PropertyMetadata(default(bool)));
        public static readonly DependencyProperty PlayerFontSizeProperty = DependencyProperty.Register("PlayerFontSize", typeof(int), typeof(TimingGrid));

        private readonly Stopwatch _playerRefresh;
        private bool _mouseOverControl;

        private IScrollInfo _scrollInfo;
        private bool _scrollToPlayerEnabled;
        private double _scrollToY;
        private Task _scrollTask;
        private DriverTimingViewModel _playerViewModel;

        public TimingGrid()
        {
            _playerRefresh = Stopwatch.StartNew();
        }

        public int PlayerFontSize
        {
            get => (int)GetValue(PlayerFontSizeProperty);
            set => SetValue(PlayerFontSizeProperty, value);
        }

        public bool HighlightPlayer
        {
            get => (bool)GetValue(HighlightPlayerProperty);
            set => SetValue(HighlightPlayerProperty, value);
        }

        public void ScrollToPlayer()
        {
            if (_mouseOverControl || !(DataContext is TimingDataGridViewModel timingDataViewModel))
            {
                return;
            }

            _playerViewModel = timingDataViewModel.DriversViewModels.FirstOrDefault(x => x.IsPlayer);

            if (_playerViewModel == null)
            {
                return;
            }

            // Find the container
            if (!(ItemContainerGenerator.ContainerFromItem(_playerViewModel) is UIElement container))
            {
                return;
            }

            ScrollContentPresenter presenter = null;
            for (Visual vis = container; vis != null && vis != this; vis = VisualTreeHelper.GetParent(vis) as Visual)
            {
                if ((presenter = vis as ScrollContentPresenter) != null)
                {
                    break;
                }
            }

            if (presenter == null)
            {
                return;
            }

            // Find the IScrollInfo
            _scrollInfo =
                !presenter.CanContentScroll ? presenter :
                presenter.Content as IScrollInfo ??
                FirstVisualChild(presenter.Content as ItemsPresenter) as IScrollInfo ??
                presenter;

            // Compute the center point of the container relative to the scrollInfo
            Size size = container.RenderSize;
            Point center = container.TransformToAncestor((Visual)_scrollInfo).Transform(new Point(0, size.Height / 2));
            center.Y += _scrollInfo.VerticalOffset;
            center.X += _scrollInfo.HorizontalOffset;

            // Adjust for logical scrolling
            if (_scrollInfo is StackPanel || _scrollInfo is VirtualizingStackPanel)
            {
                double logicalCenter = ItemContainerGenerator.IndexFromContainer(container) + 0.5;
                Orientation orientation = _scrollInfo is StackPanel ? ((StackPanel)_scrollInfo).Orientation : ((VirtualizingStackPanel)_scrollInfo).Orientation;
                if (orientation == Orientation.Horizontal)
                {
                    center.X = logicalCenter;
                }
                else
                {
                    center.Y = logicalCenter;
                }
            }

            _scrollToY = CenteringOffset(center.Y, _scrollInfo.ViewportHeight, _scrollInfo.ExtentHeight + 10);
            _scrollToPlayerEnabled = true;
            // Scroll the center of the container to the center of the viewport
            /*if (_scrollInfo.CanVerticallyScroll)
            {
                _scrollInfo.SetVerticalOffset(CenteringOffset(center.Y, _scrollInfo.ViewportHeight, _scrollInfo.ExtentHeight + 10));
            }*/

            /*if (scrollInfo.CanHorizontallyScroll)
            {
                scrollInfo.SetHorizontalOffset(CenteringOffset(center.X, scrollInfo.ViewportWidth, scrollInfo.ExtentWidth));
            }*/
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            _scrollInfo = null;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            _scrollTask = ScrollToPlayerLoop();
        }

        private async Task ScrollToPlayerLoop()
        {
            while (true)
            {
                while (_scrollInfo == null)
                {
                    ScrollToPlayer();
                    await Task.Delay(1000);
                }

                _scrollToPlayerEnabled = (this.DataContext as TimingDataGridViewModel)?.IsScrollToPlayerEnabled == true;

                if (_playerRefresh.ElapsedMilliseconds > 2000)
                {
                    _playerRefresh.Restart();
                    ScrollToPlayer();
                }

                if (_scrollInfo != null && _scrollToPlayerEnabled && !_mouseOverControl)
                {
                    double scrollTo;
                    double scrollDifference = _scrollInfo.VerticalOffset - _scrollToY;
                    if (Math.Abs(scrollDifference) <= 10)
                    {
                        _scrollInfo.SetVerticalOffset(_scrollToY);
                        await Task.Delay(500);
                        continue;
                    }

                    if (scrollDifference < 0)
                    {
                        scrollTo = _scrollInfo.VerticalOffset + 10;
                    }
                    else
                    {
                        scrollTo = _scrollInfo.VerticalOffset - 10;
                    }

                    _scrollInfo.SetVerticalOffset(scrollTo);
                }

                await Task.Delay(10);
            }
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            _mouseOverControl = true;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            SelectedItem = null;
            _mouseOverControl = false;
        }

        private static double CenteringOffset(double center, double viewport, double extent)
        {
            return Math.Min(extent - viewport, Math.Max(0, center - (viewport / 2)));
        }

        private static DependencyObject FirstVisualChild(Visual visual)
        {
            if (visual == null)
            {
                return null;
            }

            if (VisualTreeHelper.GetChildrenCount(visual) == 0)
            {
                return null;
            }

            return VisualTreeHelper.GetChild(visual, 0);
        }
    }
}