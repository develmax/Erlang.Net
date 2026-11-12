# Part 1245 — forward proven private integer arguments through tuple branches

Reuse the existing private caller analysis when a tagged tuple branch forwards an unchanged entry parameter to a tail call. Emit a literal only when all proven private callers agree. Exported functions, escaping references and conflicting callers retain the register.

The source probe checks execution, the matching and fallback paths, and the generated operands. The release close_sleep/2 instructions match OTP after label normalization. The remaining httpd_socket differences include larger stack frames in close/2 and deliver/3.

The full release run generated all 1,366 images with 357 Equal, 1,009 Different and zero rejected. Only httpd_socket changed. The immediate adds one byte to its Code size gap while matching the reference operand. OTP accepted all differing pairs.

- [Source probe](tuple_constant_forward_probe.erl)
- [Execution and instruction check](probe.txt)
- [Normalized release function](close-sleep.txt)
- [Remaining frame differences](httpd-socket-frames.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
