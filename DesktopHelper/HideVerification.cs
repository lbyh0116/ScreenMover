using System;
namespace ScreenMover.DesktopLogic {
    public enum HideOutcome { Minimized, DesktopOnly, Returned, Closed }
    public struct HideSample {
        public bool Exists, OnTarget, TargetIsCurrent, Minimized;
    }
    public static class HideVerification {
        // Some full-screen apps restore themselves after initially accepting minimization.
        public static HideOutcome Observe(Func<HideSample> read,Action<int> delay) {
            HideSample last=new HideSample();
            for(int elapsed=0;elapsed<=2500;elapsed+=100){
                last=read();
                if(!last.Exists)return HideOutcome.Closed;
                if(!last.OnTarget||last.TargetIsCurrent)return HideOutcome.Returned;
                if(elapsed<2500)delay(100);
            }
            return last.Minimized?HideOutcome.Minimized:HideOutcome.DesktopOnly;
        }
    }
}
