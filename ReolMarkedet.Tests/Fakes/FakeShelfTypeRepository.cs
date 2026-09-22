using Reolmarkedet.Core.Interfaces;
using Reolmarkedet.Core.Models;

namespace ReolMarkedet.Tests.Fakes
{
    internal class FakeShelfTypeRepository : IRepository<ShelfType>
    {
        private readonly List<ShelfType> _shelfTypes = new();
        private int _nextShelfTypeId = 1;

        public IEnumerable<ShelfType> GetAll()
        {
            return _shelfTypes;
        }

        public ShelfType? GetById(int id)
        {
            foreach (var shelfType in _shelfTypes)
            {
                if (shelfType.ShelfTypeId == id)
                {
                    return shelfType;
                }
            }

            return null;
        }

        public void Add(ShelfType shelfType)
        {
            if (shelfType.ShelfTypeId == 0)
            {
                shelfType.ShelfTypeId = _nextShelfTypeId++;
            }
            else if (shelfType.ShelfTypeId >= _nextShelfTypeId)
            {
                _nextShelfTypeId = shelfType.ShelfTypeId + 1;
            }

            _shelfTypes.Add(shelfType);
        }

        public void Update(ShelfType shelfType)
        {
            ShelfType? storedShelfType = GetById(shelfType.ShelfTypeId);
            if (storedShelfType is null)
            {
                throw new InvalidOperationException(
                    "No shelf type found with the specified ShelfTypeId");
            }

            storedShelfType.Name = shelfType.Name;

        }

        public void Delete(int id)
        {
            ShelfType? shelfType = GetById(id);

            if (shelfType is null)
            {
                throw new InvalidOperationException(
                    "No shelf type found with the specified ShelfTypeId");
            }

            _shelfTypes.Remove(shelfType);
        }
    }
}
