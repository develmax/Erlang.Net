# Part 1169 — forward a tagged result into a call and tuple

The entry selector checks its result in X0, extracts the field into the same register and invokes the conversion. The success branch constructs a tuple from the second result. The fallback returns the original selector result. The permanent probe matches OTP instructions after local label normalization and verifies both results.

The release ttb_autostart:read_config/0 instructions match including local labels. Three other functions gain an instruction match. No function loses one. The remaining normalized instruction differences in ttb_autostart are handle_info/2, init/1 and write_config/1.

The full run generated 1,363 images and returned 351 Equal, 1,012 Different and three SourceRejected. Four Code size gaps narrowed. ELDAPv3 remains Equal. OTP prepared all differing images for loading and all three focused tests passed.

- [Source audit](source-audit.txt)
- [ttb_autostart release comparison](ttb-read.txt)
- [Remaining ttb functions](ttb-remaining.txt)
- [Probe disassembly](disassembly.txt)
- [Full corpus](otp-corpus.json)
- [Image delta](delta.json)
- [Function comparison](function-comparison.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
