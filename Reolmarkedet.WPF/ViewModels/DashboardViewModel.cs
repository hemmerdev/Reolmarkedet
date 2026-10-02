using System.Windows.Threading;

namespace Reolmarkedet.WPF.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly DispatcherTimer _clockTimer;
        public DateTime CurrentDateTime => DateTime.Now;

        public DashboardViewModel()
        {
            _clockTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(1),
            };
            // Subribe to the Tick event of the timer to update the CurrentDateTime property every second
            _clockTimer.Tick += (_, _) => OnPropertyChanged(nameof(CurrentDateTime));
        }

        public void StartClock()
        {
            if (!_clockTimer.IsEnabled)
            {
                _clockTimer.Start();
                OnPropertyChanged(nameof(CurrentDateTime));
            }
        }

        public void StopClock()
        {
            if (_clockTimer.IsEnabled)
            {
                _clockTimer.Stop();
            }
        }
    }
}
