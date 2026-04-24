namespace Celeste.Mod.ExSrt.UI;

[Tracked]
public class Toast : Entity {
    private readonly string text;
    private readonly float duration;
    private float timer;

    public Toast(string text, float duration = 2.5f) {
        this.text = text;
        this.duration = duration;
        timer = duration;
        Tag = Tags.Global | Tags.HUD;
        Depth = -100000;
    }

    public override void Update() {
        base.Update();
        timer -= Engine.DeltaTime;
        if (timer <= 0f) {
            RemoveSelf();
        }
    }

    public override void Render() {
        base.Render();
        float fadeIn = Calc.ClampedMap(timer, duration, duration - 0.2f, 0f, 1f);
        float fadeOut = Calc.ClampedMap(timer, 0f, 0.25f, 0f, 1f);
        float alpha = fadeIn * fadeOut;
        Vector2 size = ActiveFont.Measure(text) * 0.7f;
        Vector2 pos = new((Engine.Width - size.X) * 0.5f, Engine.Height - 96f);
        Draw.Rect(pos.X - 12f, pos.Y - 10f, size.X + 24f, size.Y + 20f, Color.Black * (0.75f * alpha));
        ActiveFont.DrawOutline(text, pos, Vector2.Zero, Vector2.One * 0.7f, Color.White * alpha, 2f, Color.Black * alpha);
    }

    public static void Show(Scene? scene, string text) {
        if (scene == null) {
            return;
        }

        if (scene.Tracker.Entities.TryGetValue(typeof(Toast), out List<Entity>? tracked)) {
            foreach (Entity entity in tracked.ToArray()) {
                entity.RemoveSelf();
            }
        }

        scene.Add(new Toast(text));
    }
}
