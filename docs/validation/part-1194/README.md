# Part 1194 — match the ct_master_status release image

Defer record field extraction until after the first call, construct the outgoing list in registers, and trim the saved record before the second call. The emitted handle_event/2 instructions match the release. The Common Test build profile records the observed ct_event.hrl paths for both the application and bootstrap builds.

The full Windows OTP 29.1.1 corpus generated 1,363 of 1,366 images. Results were 357 Equal, 1,006 Different and 3 SourceRejected. ct_master_status moved to Equal and ELDAPv3 stayed Equal. OTP prepared all differing images for loading. Three focused compiler tests and the runtime probe passed. The probe images differ byte for byte.

- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [Focused compiler tests](focused.json)
- [Loader](loader.txt)
- [Release disassembly](disassembly.txt)
- [Probe source](deferred_record_list_probe.erl)
- [Probe assertions](run.eval)
