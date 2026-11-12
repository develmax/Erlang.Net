# Part 1415 — Project multiple call-result tuple fields

Checked call chains can pass multiple fields of a tagged or untagged tuple directly to the immediately following call. Projections follow tuple order, and the existing parallel move scheduler places them in call argument order. A proven exact tuple result from a local call skips the type and arity checks. When the proof is unavailable, the failing match retains the original value for badmatch.

The deterministic fixture includes two consecutive matches with multiple fields, a nested argument call and a local producer with a known tuple shape. The complete image matches OTP. The previous compiler differs on both consecutive-match functions. OTP executes success and failure cases, including the original non-tuple value, wrong tag and wrong arity.

The Windows OTP 29.1.1 corpus generates all 1,366 images. There are 380 Equal and 986 Different, with no source rejections or status transitions. ELDAPv3 is Equal from C#-generated Erlang source. OTP prepares all 986 differing images for loading. Seven release images changed. Eight changed functions reach the OTP frame size, and three gain normalized instruction equality with no losses. The combined Code size gap shrinks by 286 bytes. The count of Attr size differences rises from seven to eight.

The optimized plan handles fields consumed by the next call. The remaining examples in asn1ct_imm and asn1ct_parser2 use fields across a call boundary. The release shows both early projection to a stack slot and deferred projection from a saved tuple. Full runtime tests, canonical readiness regeneration and publication remain pending.

- [Corpus](otp-corpus.json), [status changes](status-changes.txt) and [section size counts](chunk-sizes.txt)
- [Normalized instructions](normalized-functions.txt), [frame sizes](frame-delta.txt) and [loader and Code sizes](loader-and-code.txt)
- [Oracle](check-tuple-fields.ps1), [probe](probe.txt), [previous compiler](previous-compiler.txt), [fixture hashes](fixture-hashes.txt) and [focused tests](focused.json)
- [Tuple field liveness in OTP](tuple-liveness.md)
