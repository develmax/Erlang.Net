# Part 1172 — share framed wx class validation returns

The wx option emitter shares one return block among all class-checking branches, even when other options separate them. A matching class skips the dynamic call and joins the common return. A branch that runs the dynamic call also joins the common return. Shape errors retain their original exit path.

The updated permanent fixture includes two nonconsecutive class branches. Its BEAM matches the pinned erlc output byte for byte. The oracle probe checks both class branches, an intervening tuple option and badoption failure.

The full run generated 1,363 images. Results were 354 Equal, 1,009 Different and three SourceRejected. wxFontPickerCtrl.beam was the only image whose status changed. No other differing image changed. ELDAPv3 remains Equal. OTP prepared all differing images for loading and all three focused tests passed.

The smallest remaining chunk difference count is five. Four wx images share the same five-chunk signature.

- [Full corpus](otp-corpus.json)
- [Image delta](delta.json)
- [Probe disassembly](disassembly.txt)
- [Probe assertions](run.eval)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Remaining chunk signatures](chunk-signatures.txt)
