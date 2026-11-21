// Delikatne „tyknięcie” przy kliknięciu (iOS 10+). Wołane z Haptics.cs.
#import <UIKit/UIKit.h>

extern "C" void FQ_HapticTap()
{
    static UIImpactFeedbackGenerator* gen = nil;
    if (gen == nil) gen = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
    [gen impactOccurred];
    [gen prepare];   // następne kliknięcie zadziała bez opóźnienia
}
