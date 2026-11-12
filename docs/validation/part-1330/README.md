# Part 1330

Emit an empty-list clause's uncaptured closure and two ordinary calls with a zero-slot frame. Reserve the fun heap space in allocate_heap and finish with a frame tail call. Reject this plan when the closure captures the clause argument.

The focused check verifies the fallback instruction sequence, the recursive prefix, improper-list function_clause arguments and a captured closure case. In the release collapse_ws/2, the allocation and call chain now match OTP. The remaining differences in that function are the explicit nil test and the module-derived fun unique value.

The full Windows corpus generated all 1,366 images with 363 Equal, 1,003 Different and zero SourceRejected. Only xmerl_xsd_type changed. Its Code size gap narrowed by 27 bytes. OTP accepted all differing images and ELDAPv3 remained Equal.

- [Focused check](check-empty-list-closure-chain.ps1)
- [Focused output](probe.txt)
- [Release comparison](xmerl-collapse-ws.txt)
- [Full corpus](otp-corpus.json)
- [Changed image](delta.json)
- [OTP loader](loader.txt)
