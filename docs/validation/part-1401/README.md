# Part 1401 — Test entry arguments before allocating wx frames

The emitter shares the guarded local forwarding plan with overloads that continue into an object command or into a protected class union and a scalar fallback. It also supports converting a middle text argument before mapping the options argument while carrying the intervening inputs through the calls.

The complete deterministic fixture matches OTP. The oracle verifies local forwarding, the protected class union, a missing environment, rejected guard arguments and invalid options. The three selected compiler tests pass with the corpus compiler assembly.

The initial implementation intercepted overloads with an existing shared record test. The whole-module regression caught the duplicate test and the initial corpus found ten previously equal wx images that differed. Giving the shared-record emitter precedence restores all ten images and the regression fixture.

The final corpus generated all 1,366 release images, with 375 Equal, 991 Different, zero SourceRejected and zero Unsupported. wxCursor, wxStaticBoxSizer and wxStyledTextCtrl are newly equal. Ten images changed, five functions gained normalized instruction matches and none lost them. The combined Code gap shrank by 1,996 bytes. OTP prepared all 991 differing images for loading.

The paired colour guards in wxListItemAttr:new/3 still enter the general frame emitter. OTP checks both colours before allocating, then reuses a slot between the two colour conversions. The source audit found paired colour guards in wxCalendarCtrl, wxDC, wxGraphicsContext and wxListItemAttr.

- [Complete corpus](otp-corpus.json)
- [Status changes](status-changes.txt)
- [Release targets](release-targets.txt) and [regression targets](regression-targets.txt)
- [Instructions before](operands-before.txt) and [after](operands-after.txt)
- [Normalized function changes](normalized-functions.txt) and [loader and Code sizes](loader-and-code.txt)
- [Oracle](check-wx-guarded-forward.ps1), [result](probe.txt) and [hashes](fixture-hashes.txt)
- [Initial compiler tests](initial-focused.json) and [final compiler tests](focused.json)
- [Paired colour guards](paired-colour-guards.txt)
