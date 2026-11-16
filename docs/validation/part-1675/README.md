# Part 1675

When a tagged record is needed as a whole before its field is passed to a second call, keep only the record in the frame. Project the field into X0 after the first call and release the frame slot.

The run/1 instructions match OTP. The fixture images differ because consume/1 still uses a frame in C#. Both images preserve the call order and reject a mismatching record before performing side effects. Three focused checks passed. [Fixture](beam_deferred_record_field.erl) · [Instructions](disassembly.txt) · [SHA-256](fixture-sha256.txt) · [VM](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 398 Equal and 968 Different. ELDAPv3 remains byte-identical. Only wx.beam changed. Its set_env/1 instructions now match OTP after label normalization. The OTP loader accepted each differing image. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt).

The full runtime suite and readiness regeneration were outside the validation scope.
