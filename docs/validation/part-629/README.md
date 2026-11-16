# Part629 LDAP INTEGER and NULL codec bodies

The C# ASN.1 generator now emits direct INTEGER and NULL codec calls for MessageID, AbandonRequest and UnbindRequest. It resolves implicit tags and aliases before selecting the primitive body, which also enables direct OCTET STRING handling for DelRequest. BER tags remain caller supplied in the arity-two interface. Integer decoding preserves the range check and uses the shared tag matcher.

The installed OTP VM loaded the generated module and matched the release across 89 direct calls. The comparison includes byte and length output, wrong tags, nested tags, range errors, signed values, large integers, NULL values and an implicit OCTET STRING alias. Release byte parity remains open.

All three focused tests passed. The final 1,366-image corpus recorded 18 exact matches, ten differing images, 488 source rejections and 850 unsupported modules. [Per-image report](otp-corpus.json).
