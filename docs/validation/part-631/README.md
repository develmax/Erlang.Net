# Part631 required LDAP SEQUENCE codec bodies

The C# ASN.1 writer emits direct required-field SEQUENCE codecs for AttributeValueAssertion, SearchResultEntry, AddRequest, CompareRequest and RealSearchControlValue. The encoder matches tuple arity, encodes each component and accumulates lengths in source order. The decoder matches caller supplied tags, consumes required fields and skips trailing extensions for extensible sequences.

The installed OTP VM loaded the generated file and matched the release across 25 calls comparing BER bytes and lengths, values and error reasons. Cases cover tagged encoding, malformed record arity, missing fields and trailing extensions. Release byte parity remains open.

Five additional record round trips matched the release. All three focused tests passed. The final 1,366-image corpus recorded 18 exact matches, eleven differing images, 488 source rejections and 849 unsupported modules. [Per-image report](otp-corpus.json).
