# Part901 Dynamic, float and Unicode bit patterns

The general BEAM emitter can extract dynamic-width integer and binary fields, floating-point fields and Unicode codepoints. Extraction runs in source order, so a later field can use a binding made by an earlier field. The emitter checks the remaining bit count for an exact pattern or checks alignment before extracting an unsized tail. Fixed-width patterns keep the existing combined match instructions.

The regression exercises a signed field whose width comes from the preceding segment, a non-byte-aligned tail, a UTF-8 codepoint, a little-endian float and fallback on malformed input or extra bits.

All three focused compiler checks passed. The generated regression image loaded on OTP and returned the same four results as the pinned compiler output. [Focused report](focused.json).

The complete 1,366-image run found 137 Equal, 812 Different, 105 SourceRejected and 312 Unsupported. Forty modules moved from Unsupported to Different. ELDAPv3 remained Equal using the source generated from ASN.1 by C#. [Corpus report](otp-corpus.json).
