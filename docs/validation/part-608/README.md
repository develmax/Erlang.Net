# Part608 preserved argument across an external call

The C# backend now lowers a two-clause function with an atom in its first parameter and a catch-all fallback. It preserves the second parameter in a stack slot across a static external call. The success branch extracts a tagged tuple member and constructs two tuples with the preserved parameter. The error branch returns the call result directly.

The release metadata writer serializes each unguarded function clause with its own annotation and emits the arity signature for multi-clause functions in Docs. The mnesia_app profile reuses the Mnesia compiler version, build paths and options. The retained fixture hashes match the admitted Windows x64 release inventory.

All 1,780 bytes of mnesia_app match the installed release. An isolated original OTP VM loaded the generated image and exercised successful start, repeated start returning the already-started error, bad start arguments and stop. Eight focused tests passed, including the 14 release fixtures and 27 deterministic fixtures. [Focused report](focused.json).

The full OTP 29.1.1 corpus command exited 1 after checking all 1,366 images. It recorded 14 byte matches, 850 unsupported modules, 501 source rejections and one missing source. Full regression and canonical readiness remain open. [Per-image report](otp-corpus.json).
