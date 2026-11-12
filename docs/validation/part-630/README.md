# Part630 LDAP collection codec bodies

The C# ASN.1 writer emits direct collection encoders and decoders for Referral, Controls, AttributeSelection, PartialAttributeList, SearchResultReference and AttributeList. Each encoder uses a components helper adjacent to the parent codec, accumulates byte fragments and lengths, and applies caller supplied tags. The decoder uses the shared tag matcher and element decoder. The C# BEAM emitter lowers the new match-and-call and comprehension forms.

The original OTP VM loaded the generated image and matched the release across 54 direct calls, comparing BER bytes and lengths, decoded values and error reasons. Inputs include empty collections, record and string elements, external tags, wrong tags and malformed input. The release BEAM remains byte-different.

All three focused tests passed. The final 1,366-image corpus recorded 18 exact matches, ten differing images, 488 source rejections and 850 unsupported modules. [Per-image report](otp-corpus.json).
