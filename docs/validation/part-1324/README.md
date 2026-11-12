# Part 1324

For an unguarded tuple clause followed by a constant fallback, test the tuple before allocating a frame when all other pattern tests are already satisfied. Emit the constant fallback as a direct return. Repeated bindings still use normal clause matching.

The focused test compares the entry tests and fallback against OTP and exercises wrong arity, wrong tag, shared literal rejection and repeated bindings. In asn1ct:save_gen_state/2 the tuple tests and fallback now match the release. The main branch still allocates six slots against one in OTP and uses copy for the record update.

The full Windows corpus generated all 1,366 images with 363 Equal, 1,003 Different and zero SourceRejected. The Code size gap narrowed by 16 bytes in asn1ct and 14 in observer_app_wx. No complete release function instruction match changed after label normalization. OTP accepted all differing images and ELDAPv3 remained Equal.

- [Focused check](check-tuple-before-frame.ps1)
- [Focused output](probe.txt)
- [Release function comparison](asn1-save-gen-state.txt)
- [Full corpus](otp-corpus.json)
- [Changed images](delta.json)
- [OTP loader](loader.txt)
