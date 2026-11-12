# Part626 LDAP integer bounds and common function order

The OTP ASN.1 oracle exposed an integer range mismatch. In the release, encoding accepts out-of-range integers and decoding rejects them with bad_range. The C# generator now retains finite INTEGER ranges, resolves named bounds such as maxInt, and emits a check for bounded integer decoding. The BEAM emitter also recognizes the OTP ASN.1 exception normalization shape so public encode and decode expose the original ASN.1 reason.

The C# generator groups encoder and decoder exports and places the shared public entry points, type codecs, values and Dialyzer export in the same relative order as OTP. Specialized OTP encoder and decoder bodies and private helper order still differ. The C# and release BEAM files remain byte-different.

The OTP parser confirmed the same relative order for 109 shared functions. The generated BEAM loaded in OTP and matched the release in nine range cases, comparing BER bytes, decoded values and normalized error reasons.

All three focused tests passed. The final 1,366-image corpus recorded 18 exact matches, nine differing images, 488 source rejections and 851 unsupported modules. [Per-image report](otp-corpus.json).
