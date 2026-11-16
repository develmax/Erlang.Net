# Part 920 binary comprehensions

The general emitter shares qualifier traversal with list and map comprehensions. It initializes the writable bitstring with bs_init_writable and adds output segments through the private_append form of bs_create_bin. Existing guard and pattern handling determine which inputs append output. Direct bit construction keeps the original segment numbering, while the comprehension output numbers the accumulator first.

The pinned OTP probe covers dynamic bit widths, relaxed pattern mismatches, filter failures, non-byte-aligned concatenation, empty output and error reasons. [Reference run and disassembly](reference.txt).

The existing general loop keeps iteration inside the parent function. OTP uses a lifted helper for the examined forms. Binary generator patterns and zipped generators still need separate lowering.

All three selected compiler tests passed, including the five OTP reference outcomes in the managed runtime. [Focused report](focused.json).

The full Windows OTP 29.1.1 run reported 221 Equal, 809 Different, 105 SourceRejected and 231 Unsupported across 1,366 files. asn1ct_value and eval_bits moved from Unsupported to Different. The three Megaco PER modules reached a binary generator in decode_named_bit_string/2, and ssl_handshake reached decode_extensions/4. No status regressed. [Corpus report](otp-corpus.json).
