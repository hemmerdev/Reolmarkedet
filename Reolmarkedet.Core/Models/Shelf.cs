using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Reolmarkedet.Core.Models
{
    public class Shelf : INotifyPropertyChanged
    {
        public int ShelfId { get; set; }
        public int ShelfNumber { get; set; }
        public string? RowLabel { get; set; }
        public int? PositionInRow { get; set; }
        public bool IsActive { get; set; } = true;
        public ShelfType ShelfType
        {
            get => _shelfType;
            set
            {
                if (_shelfType != value)
                {
                    _shelfType = value;
                    OnPropertyChanged(nameof(ShelfType));
                }
            }
        }
        private ShelfType _shelfType;

        public Shelf(ShelfType shelfType)
        {
            _shelfType = shelfType;

        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
