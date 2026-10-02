using Reolmarkedet.WPF.Services;

namespace ReolMarkedet.Tests.Fakes
{
    public class FakeConfirmationService : IConfirmationService
    {
        public bool Confirm(string message)
        {
            return true;
        }
    }
}
