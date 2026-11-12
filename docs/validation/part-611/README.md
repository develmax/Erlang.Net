# Part611 release function specs and dets_sup parity

The release metadata writer serializes a single unguarded function spec without named arguments. It reuses the source type parser and emits OTP abstract forms with annotated fun and product nodes. Types remain absent from the runtime Attr chunk. The existing constant folding and nil-pattern lowering already cover dets_sup:init/1. This change adds the stdlib release compiler options for the module.

The generated dets_sup image matched all 2,168 bytes of the admitted Windows x64 release. The fixture source and reference image hashes match the admitted inventory. A reference OTP VM loaded the generated BEAM and exercised successful init with an empty list and the function_clause path with an invalid argument.

Both builds and all eight selected tests passed. [Focused report](focused.json).

The corpus run checked 1,366 images. It exited 1 with 16 exact matches, 488 source rejections and 862 modules outside the current BEAM emission profile. No module has a missing source. Full regression and canonical readiness remain open. [Per-image report](otp-corpus.json).
