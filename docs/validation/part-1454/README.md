# Part 1454 — Group literal clauses with computed fallbacks

The C# compiler now reuses the literal jump table for functions with distinct atom or integer keys, constant branch results and a computed final clause. The table leaves the input in x0 for the fallback. For a binary constructor with a constant prefix and a known binary conversion, the fallback merges the prefix into one string segment, consumes the call result in x0, and records the binary alignment in the Type chunk.

erl_posix_msg.beam matches the installed Windows OTP 29.1.1 image byte for byte. After matching all other chunks, the last difference was the unit flag and alignment byte in Type. The matched image is 14,084 bytes, compared with 14,796 before this part.

The initial broad rewrite changed existing identity and constant-error fallback schedules. The extractor now leaves those cases with the existing emitters. Embedded release fixtures protect erl_posix_msg.beam and crypto_ec_curves.beam. The OTP instruction fixture uses iolist_to_binary/1 for a binary fallback executable by both runtimes. The installed erl_posix_msg fixture retains atom_to_binary/1.

- [Isolated release comparison](isolated-release-images.json), [Type chunk evidence](posix-before-type-unit.txt), and [fixture instructions and results](probe.txt)

The final emitter reuses frameless construction and tail calls for eligible fallbacks. The full Windows release run generated 1,366 images with zero source rejections. It records 383 Equal and 983 Different, with ELDAPv3 still Equal. The only status change is erl_posix_msg from Different to Equal. Forty-six images changed, and 19 functions gained normalized instruction equality with zero losses. Both loaders accepted all 983 differing images.

The check_purge/1 fallback now avoids a frame, with an instruction difference in the throw path. OTP emits raise/3 after proving the stack is unobserved, while C# emits throw/1. The next group to investigate combines atom and arity tests, starting with erl_internal:bif/2.

- [Full corpus](otp-corpus.json), [focused tests](focused.json), and [compiler hash](compiler-hash.txt)
- [Normalized instruction changes](normalized-functions.txt), [loader and code checks](loader-and-code.txt), and [chunk comparison](chunks-after.txt)
- [Remaining fallback difference](check-purge-fallback.txt), [atom jump-table ranking](atom-jump-table-ranking.txt), and [erl_internal:bif/2 instructions](erl-internal-bif.txt)
