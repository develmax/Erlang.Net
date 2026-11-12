# Part 1319

The nested argument planner builds a dynamic tuple and its nested tuple or singleton list in X registers before a one-argument tail call. It reserves the heap in one instruction, reuses dead argument registers and preserves ordinary local and external call resolution. The focused test compares the full stub/3 instruction stream and executes the exception and local forwarding paths against OTP 29.1.1.

The Windows corpus produced all 1,366 images with 363 Equal, 1,003 Different and zero SourceRejected. All nine changed images narrowed their Code size gap. Nine additional functions match after label normalization. OTP accepted all 1,003 differing images. ELDAPv3 remains Equal.

The error_handler Code chunk fell from 781 to 760 bytes against 727 in the release. stub_function/3 matches after label normalization. ensure_loaded/1 and call_undefined_function_handler/3 still differ in case lowering and register allocation. The other two previously rejected modules, test_server_node and merl_transform, still generate differing images.

Use the [complete source overlay](../part-972/inputs.md) when reproducing the corpus. An initial run used a header-only overlay and recorded seven missing-document errors. The repeated run used the complete overlay.

- [Focused check](check-nested-unary-tail.ps1)
- [Focused output](probe.txt)
- [Full corpus](otp-corpus.json)
- [Changed images](delta.json)
- [Function comparison](normalized-functions.txt)
- [OTP loader](loader.txt)
- [Opcode ranking](opcode-ranking.txt)
