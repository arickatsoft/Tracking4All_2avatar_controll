#import <AVFoundation/AVFoundation.h>
#import <Foundation/Foundation.h>
#include <atomic>

static AVSpeechSynthesizer *speaker;
static std::atomic<int> speechStatus(0);
static void OnMain(void (^work)(void)) {
    if ([NSThread isMainThread]) work(); else dispatch_async(dispatch_get_main_queue(), work);
}
extern "C" void FitFightSpeechInitialize() {
    OnMain(^{
        if (!speaker) speaker = [[AVSpeechSynthesizer alloc] init];
        speechStatus = [AVSpeechSynthesisVoice voiceWithLanguage:@"en-US"] ? 1 : -1;
    });
}
extern "C" int FitFightSpeechStatus() { return speechStatus.load(); }
extern "C" void FitFightSpeechSpeak(const char *text) {
    NSString *message = [NSString stringWithUTF8String:text ? text : ""];
    OnMain(^{
        if (!speaker || speechStatus != 1) return;
        [speaker stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];
        AVSpeechUtterance *utterance = [AVSpeechUtterance speechUtteranceWithString:message];
        utterance.voice = [AVSpeechSynthesisVoice voiceWithLanguage:@"en-US"];
        utterance.rate = AVSpeechUtteranceDefaultSpeechRate;
        [speaker speakUtterance:utterance];
    });
}
extern "C" void FitFightSpeechStop() {
    OnMain(^{ [speaker stopSpeakingAtBoundary:AVSpeechBoundaryImmediate]; });
}
extern "C" void FitFightSpeechShutdown() {
    OnMain(^{
        [speaker stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];
#if !__has_feature(objc_arc)
        [speaker release];
#endif
        speaker = nil; speechStatus = 0;
    });
}
