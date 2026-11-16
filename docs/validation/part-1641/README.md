# Part 1641

The compiler reuses the call argument construction planner for a tuple or list bound before a tail call when the binding contains one producer call and the continuation constructs a distinct argument. It keeps the producer result in X0, reserves heap for all constructors together, and stacks only parameters needed after the producer. OTP 29.1.1 release disassembly is the reference. No dependency was added.

The deterministic fixture is byte-identical. The OTP VM verified the constructed arguments and three focused checks passed. [Instructions](disassembly.txt) · [Hashes](fixture-sha256.txt) · [VM](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 395 Equal and 971 Different. ELDAPv3 remains byte-identical. The change altered 1 images. The OTP loader accepted each differing image. The instructions of diameter_service:start_link/1 match after label normalization. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt) · [Diameter](diameter-service-disassembly.txt).

The initial broader rule caused a normalized instruction regression in an ASN.1 compiler closure. The narrower rule preserves the existing lowering for continuations without a distinct constructor. The full runtime suite and readiness regeneration were outside the validation scope.
