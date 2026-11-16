# Part 1150 — project tagged tuple fields into tail argument registers

The terminal case planner extracts a tagged tuple field into its final outgoing argument register. If the call uses the field more than once, the planner copies from that register into the other argument registers, including X0.

Both OTP 29.1.1 and the generated probe returned the expected value from the tagged branch and the fallback. The tagged_tail/2 instructions match after local label normalization. In asn1ct, extraction now writes directly to X3. Two independent Y-to-X moves still occur in a different order.

The full run generated 1,363 images. It returned 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP prepared all differing images for loading. Four images changed and have smaller Code size gaps. The instructions in code_server:absname/1 gained a match after local label normalization.

- [Full corpus](otp-corpus.json)
- [Image delta](delta.json)
- [Changed images](changed-images.txt)
- [Function comparison](function-comparison.txt)
- [Loader results](loader.txt)
- [Probe source](beam_general_catch_probe.erl)
- [Probe assertions](run.eval)
- [Probe disassembly](disassembly.txt)
- [asn1ct comparison](asn1ct-test-value.txt)
- [Focused tests, 3 passed](focused.json)
