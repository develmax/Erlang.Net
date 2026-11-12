# Part 1237 — type repeated list bindings and share extraction across constructed calls

Reuse the constructed-argument planner in both nonempty branches of a three-clause list scan. Validate both branches before emitting their shared list extraction. A failed plan falls back to the general emitter.

Prove integer bounds for a private argument when all incoming calls supply literals or preserve the argument across direct recursion. Exported entries and escaping function references keep untyped operands. The release test_server_sup:from/2 now matches the OTP instructions after label normalization, including the integer bounds 46 and 64.

The full release run generated 1,366 images with 357 Equal, 1,009 Different and zero rejected. The constructed branch change alone changed zero release images. The type annotation changed test_server_sup and increased its Code size gap by two bytes. OTP accepted all differing pairs.

- [Execution and operand probe](probe.txt)
- [Constructed branch fixture](list_head_constructed_probe.erl)
- [Integer bounds fixture](list_head_type_probe.erl)
- [Normalized release instructions](test-server-sup-from.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
