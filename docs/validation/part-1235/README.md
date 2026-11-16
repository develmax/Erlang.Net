# Part 1235 — retain entry arguments in register-only list scans

Extend the three-clause list scan to retain arguments in other X registers and to accept the list at any argument index. Emit exact comparisons for repeated bindings in the matching clause. Use the existing argument move scheduler to prepare the local tail call, including register swaps.

The execution probe covers exact numeric equality, repeated tuple bindings, a swapped pair of arguments and the function_clause path for an improper tail. The release test_server_sup:from/2 instructions match after normalizing labels and the remaining register type annotation.

The full release run generated 1,366 images with 357 Equal, 1,009 Different and zero rejected sources. Eighteen images changed. Their total Code size gap fell by 851 bytes. OTP accepted all differing image pairs.

- [Execution probe](probe.txt)
- [Probe source](list_head_args_probe.erl)
- [Release function disassembly](test-server-sup-from.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
