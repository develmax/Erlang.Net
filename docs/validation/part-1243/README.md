# Part 1243 — align abstract annotations and Unicode binary strings

Preserve the source text for exactly quoted configurable keywords in abstract expressions and included types. Use the first token inside grouping parentheses for subsequent list elements. Reconstruct UTF string segments from source tokens, retaining the order of endian and encoding modifiers.

The source fixture matches the OTP forms after path normalization. The strict release probe confirms byte-identical Dbgi in dialyzer_plt, yecc and the UTF-8 and two UTF-16 xmerl SAX parsers.

The full corpus generated all 1,366 images with 357 Equal, 1,009 Different and zero rejected. Only Dbgi changed across 69 modules. Forty chunks newly match the release and five previously missing chunks were restored. OTP accepted all differing image pairs. The remaining strict abstract-form failures are filelib, qlc and qlc_pt.

- [Abstract form comparison](probe.txt)
- [Probe source](debug_feature_atom_probe.erl)
- [Strict release comparison](abstract-probe.txt)
- [Full corpus](otp-corpus.json)
- [Chunk delta](debug-delta.txt)
- [Remaining strict failures](remaining-strict.txt)
- [OTP loader](loader.txt)
