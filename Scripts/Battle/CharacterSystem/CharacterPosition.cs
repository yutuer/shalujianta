using Godot;

public partial class CharacterPosition : Node2D
{
    [Export]
    public int PositionIndex;

    [Export]
    public CharacterDefinition Character;

    [Export]
    public int CurrentRage { get; set; } = 0;

    public const int MaxRage = 100;

    private bool isAnimating = false;

    public override void _Ready()
    {
    }

    public void AddRage(int amount)
    {
        CurrentRage = Mathf.Min(CurrentRage + amount, MaxRage);
    }

    public void ResetRage()
    {
        CurrentRage = 0;
    }

    public bool CanUseUltimate()
    {
        return CurrentRage >= MaxRage;
    }

    public void UseUltimate(Player player, Enemy[] enemies)
    {
        if (!CanUseUltimate()) return;

        if (Character?.UltimateSkillId != null)
        {
            UltimateSkill skill = UltimateSkill.GetUltimateById(Character.UltimateSkillId);
            skill.Execute(player, enemies);
        }

        CurrentRage = 0;
    }

    public void PlayAttackAnimation()
    {
        if (isAnimating) return;
        isAnimating = true;

        Tween tween = CreateTween();
        Vector2 originalPos = Position;
        tween.TweenProperty(this, "position", new Vector2(originalPos.X + 20, originalPos.Y), 0.1f);
        tween.TweenProperty(this, "position", originalPos, 0.1f);
        tween.TweenCallback(new Callable(this, nameof(OnAnimationComplete)));

        tween.Play();
    }

    public void PlayDamagedAnimation()
    {
        if (isAnimating) return;
        isAnimating = true;

        Tween tween = CreateTween();
        var originalModulate = Modulate;

        tween.TweenProperty(this, "modulate", new Color(1f, 0.3f, 0.3f), 0.1f);
        tween.TweenProperty(this, "modulate", originalModulate, 0.1f);
        tween.TweenCallback(new Callable(this, nameof(OnAnimationComplete)));

        tween.Play();
    }

    public void PlayIdleAnimation()
    {
    }

    public void PlayUltimateAnimation(Player player, Enemy[] enemies)
    {
        if (isAnimating) return;
        isAnimating = true;

        Tween tween = CreateTween();
        tween.TweenProperty(this, "modulate", new Color(1f, 1f, 0.3f, 1f), 0.2f);
        tween.TweenProperty(this, "modulate", Modulate, 0.3f);
        tween.TweenCallback(new Callable(this, nameof(OnAnimationComplete)));

        tween.Play();
    }

    private void OnAnimationComplete()
    {
        isAnimating = false;
    }

    public bool IsAnimating()
    {
        return isAnimating;
    }
}
