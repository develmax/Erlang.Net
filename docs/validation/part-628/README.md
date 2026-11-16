# Part628 LDAP OCTET STRING codec bodies

The C# ASN.1 generator emits the direct encoder and decoder bodies used by the OTP OCTET STRING codecs for nine LDAP types. It resolves aliases before selecting these bodies. The shared helpers use BER lengths and the tag nesting order, handle constructed strings, and report the OTP wrong_tag reason. Part collection accumulates fragments before combining them. The C# BEAM emitter routes the nested calls and the match followed by case through its general emitter.

The installed release image remains byte-different. Other type bodies, the complete private helper sequence, instruction selection and release metadata are still open.

The final C# image loaded in OTP. It matched all 180 direct calls on the nine codecs, comparing BER bytes, returned lengths, decoded values and error reasons.

The OTP parser confirmed the same relative order for eight shared helpers. All three focused tests passed. The final 1,366-image corpus recorded 18 exact matches, ten differing images, 488 source rejections and 850 unsupported modules. [Per-image report](otp-corpus.json).
