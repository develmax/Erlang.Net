# Part 1132 — align slot clearing and selector tuple types

The call setup emitter places the final move to X0 after slot clearing when the move reads no cleared slot. The tagged selector uses a tuple type operand when local return analysis proves that the selector returns a tuple.

The staged/2 probe still matches, including the case where the final move must read the slot before it is cleared. Three released functions gained instruction matches after local label normalization. One is the asn1ct test_module closure.

- [Probe](disassembly.txt)
- [Release comparison](asn1ct-test-module.txt)
- [Function comparison](function-comparison.txt)

The full run generated 1,363 images. Results were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP prepared all differing images for loading. Absolute Code size gaps widened in all three changed images.

The frame classifier compares base opcode sequences and tracks paired allocate sizes. It found 778 modules with at least one move versus init_yregs mismatch where the generated frame is larger and OTP is saving an argument. The groups overlap when a module has more than one kind of mismatch.

- [Frame groups](frame-groups.txt)
- [Frame classifier](frame-groups.eval)
- [Focused tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Loader results](loader.txt)
