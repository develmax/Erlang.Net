# Part 1185 — plan the first call after a consumed entry guard

The entry-frame profile separates early pattern tests from initial stack copies. In 696 differing images, at least one function starts with an X-to-Y move in OTP but initializes a frame at least three slots larger in the C# output. Cohort image counts overlap.

The general emitter now applies first-call argument liveness after the entry guard has been fully emitted in X registers. The probe passed on both OTP and generated output, including function_clause arguments when the guard fails. The generated frame shrank from three slots to two. OTP uses one and keeps the call result in X0.

The full Windows OTP 29.1.1 corpus generated 1,363 of 1,366 images. Results were 356 Equal, 1,007 Different and 3 SourceRejected. ELDAPv3 remained Equal. All 63 changed images moved closer in Code length. OTP prepared all differing images for loading. Three focused tests passed.

- [Frame cohorts](frame-cohorts.txt)
- [Cohort program](frame-cohorts.eval)
- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Probe source](guard_first_call_probe.erl)
- [Probe assertions](run.eval)
- [Before](before.txt)
- [After](after.txt)
- [OTP](otp.txt)
