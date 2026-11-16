# Part 1246 — discard protected branch results without saving dead values

For a tagged case branch that discards a caught call and returns a literal, save only the call's live entry arguments and the handler token. Clear the saved arguments once they reach the call registers. Use the try and handler path to discard both normal and exceptional results.

The execution probe verifies a normal return, error, throw, exit and the case fallback. The release deliver/3 instructions match OTP after label normalization, including its three-slot frame.

The full release run generated all 1,366 images with 357 Equal, 1,009 Different and zero rejected. Only httpd_socket changed. Its module Code size gap grew by two bytes despite the instruction match in deliver/3. The close/2 instruction difference remains. OTP accepted all differing image pairs.

- [Probe source](discarded_branch_catch_probe.erl)
- [Execution and frame check](probe.txt)
- [Normalized release function](deliver.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
