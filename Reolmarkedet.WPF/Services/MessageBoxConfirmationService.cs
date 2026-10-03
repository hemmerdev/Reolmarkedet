using System.Windows;

namespace Reolmarkedet.WPF.Services
{
    public class MessageBoxConfirmationService : IConfirmationService
    {
        public bool Confirm(string message)
        {
            var result = MessageBox.Show(
                message,
                "Bekræft handling",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            return result == MessageBoxResult.Yes;
        }
    }
}
