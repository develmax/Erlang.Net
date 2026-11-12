# Part 1042 register-only tail calls

The general fallback now emits a direct tail call for an unguarded clause with unconditional parameters when every outgoing argument is an incoming variable or a constant. It reuses the existing parallel register move emitter and static call resolution. Calls that need general expression evaluation still use the general frame path.

The closure probe returned the same reordered tuple before and after the change and with OTP-generated code. Its lifted body now uses two swaps and call_only instead of four Y slots and call_last. OTP emits the swaps in the opposite order. The asn1ct compile closure now uses call_only but still has an extra swap due to capture ordering.

Across the differing images, the number of matched functions where the generated code had an ordinary allocation and OTP had none fell from 24411 to 22243. The number of modules with at least one such function fell from 947 to 944. The opposite category, where only OTP had an ordinary allocation, rose from 10 to 13 functions.

All 1021 differing images still pass OTP loader preparation. The full corpus admitted and generated 1363 of 1366 images. Per-file statuses remained unchanged at 342 Equal, 1021 Different and 3 SourceRejected. ELDAPv3 remains Equal.

- [Full corpus](otp-corpus.json)
- [Focused checks](focused.json)
- [Frame counts](frames.txt)
- [Loader results](loader.txt)
- [asn1ct lifted body](asn1ct-tail.txt)
- [Probe source](tail_frame_probe.erl)
- [Generated probe before](probe-before.txt)
- [Generated probe after](probe-after.txt)
- [OTP probe](probe-otp.txt)
