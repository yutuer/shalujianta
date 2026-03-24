using Godot;
using System.Collections.Generic;
using FishEatFish.Battle.Core;

namespace FishEatFish.Battle.Effects;

public partial class EventTriggerSystem : Node
{
    private static EventTriggerSystem _instance;
    public static EventTriggerSystem Instance => _instance;

    private Dictionary<EventType, List<EffectInstance>> _registry = new Dictionary<EventType, List<EffectInstance>>();

    public override void _Ready()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            QueueFree();
        }
    }

    public void RegisterEffect(EffectInstance effect)
    {
        var eventType = ConvertToEventType(effect.Template.Trigger);
        if (!_registry.ContainsKey(eventType))
        {
            _registry[eventType] = new List<EffectInstance>();
        }
        _registry[eventType].Add(effect);
    }

    public void UnregisterEffect(EffectInstance effect)
    {
        var eventType = ConvertToEventType(effect.Template.Trigger);
        if (_registry.TryGetValue(eventType, out var effects))
        {
            effects.Remove(effect);
        }
    }

    public void Trigger(EventType eventType, EffectContext context)
    {
        if (_registry.TryGetValue(eventType, out var effects))
        {
            foreach (var effect in effects)
            {
                if (!effect.CanTrigger(context))
                {
                    continue;
                }

                effect.Apply(context);
            }
        }
    }

    public void OnTurnStart()
    {
        var context = new EffectContext { CurrentTrigger = TriggerType.OnTurnStart };
        Trigger(EventType.OnTurnStart, context);
    }

    public void OnTurnEnd()
    {
        var context = new EffectContext { CurrentTrigger = TriggerType.OnTurnEnd };
        Trigger(EventType.OnTurnEnd, context);
    }

    public void OnDamageReceived(IUnit target, IUnit attacker, int damage)
    {
        var context = new DamageContext
        {
            Target = target,
            Source = attacker,
            Owner = target,
            Damage = damage,
            CurrentTrigger = TriggerType.OnDamageReceived
        };
        Trigger(EventType.OnDamageAfter, context);
    }

    private EventType ConvertToEventType(TriggerType trigger)
    {
        return trigger switch
        {
            TriggerType.OnTurnStart => EventType.OnTurnStart,
            TriggerType.OnTurnEnd => EventType.OnTurnEnd,
            TriggerType.OnDamageDealt => EventType.OnDamageAfter,
            TriggerType.OnDamageReceived => EventType.OnDamageAfter,
            TriggerType.OnHeal => EventType.OnHeal,
            TriggerType.OnKill => EventType.OnKill,
            _ => EventType.OnTurnStart
        };
    }
}
