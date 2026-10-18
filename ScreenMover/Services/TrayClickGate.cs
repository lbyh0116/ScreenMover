namespace ScreenMover.Services;

// One outside mouse-down on the tray must not reopen the panel on mouse-up.
public sealed class TrayClickGate {
    long closedAt=long.MinValue;
    public void Reset()=>closedAt=long.MinValue;
    public void DismissFromTray(long now)=>closedAt=now;
    public bool Consume(long now){
        bool consume=closedAt!=long.MinValue&&now>=closedAt&&now-closedAt<=1500;
        closedAt=long.MinValue;return consume;
    }
}
