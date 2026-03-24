using System.Collections.Generic;
using FishEatFish.Battle.Core;

namespace FishEatFish.Battle.Effects;

public class EntityManager
{
    private static EntityManager _instance;
    public static EntityManager Instance => _instance ??= new EntityManager();

    private Dictionary<int, IUnit> _entities = new Dictionary<int, IUnit>();

    private EntityManager() { }

    public void Register(IUnit entity)
    {
        _entities[entity.Id] = entity;
    }

    public void Unregister(int id)
    {
        _entities.Remove(id);
    }

    public bool Exists(int id)
    {
        return _entities.ContainsKey(id) && !_entities[id].IsDead;
    }

    public IUnit GetUnit(int id)
    {
        return _entities.TryGetValue(id, out var unit) ? unit : null;
    }
}
