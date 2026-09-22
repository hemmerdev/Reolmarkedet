using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;

namespace ReolMarkedet.Tests.Fakes
{
    internal class FakeShelfRepository : IRepository<Shelf>
    {
        private readonly List<Shelf> _shelves = new();
        private int _nextShelfId = 1;

        public IEnumerable<Shelf> GetAll()
        {
            return _shelves;
        }

        public Shelf? GetById(int id)
        {
            foreach (var shelf in _shelves)
            {
                if (shelf.ShelfId == id)
                {
                    return shelf;
                }
            }

            return null;
        }

        public void Add(Shelf shelf)
        {
            if (shelf.ShelfId == 0)
            {
                shelf.ShelfId = _nextShelfId++;
            }
            else if (shelf.ShelfId >= _nextShelfId)
            {
                _nextShelfId = shelf.ShelfId + 1;
            }

            _shelves.Add(shelf);
        }

        public void Update(Shelf shelf)
        {
            Shelf? storedShelf = GetById(shelf.ShelfId);
            if (storedShelf is null)
            {
                throw new InvalidOperationException(
                    "No shelf found with the specified ShelfId");
            }

            storedShelf.ShelfNumber = shelf.ShelfNumber;
            storedShelf.IsActive = shelf.IsActive;
            storedShelf.ShelfType = shelf.ShelfType;
        }

        public void Delete(int id)
        {
            Shelf? shelf = GetById(id);

            if (shelf is null)
            {
                throw new InvalidOperationException(
                    "No shelf found with the specified ShelfId");
            }

            _shelves.Remove(shelf);
        }
    }
}
