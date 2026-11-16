# Part650 LDAP null and integer encoding and ASN.1 export coordinates

The C# emitter follows the OTP move order for a local tail call that forwards the second input and fills two argument registers. A guarded integer conversion followed by a local tail call now uses a single saved register. The fallback uses the existing frameless tagged exit emitter. The managed test covers the empty payload, integer result and nested error payload.

The pinned OTP 29.1.1 disassembly matches for encode_null/2 and encode_integer/2 after normalizing source lines and local labels. The functions have 8 and 19 instructions respectively. The image has 10,557 instructions against 7,028 in the release. AtU8, ImpT, StrT, CInf and Meta remain byte-exact. [Encoder comparison](encoder-oracle.txt), [chunk comparison](chunks-oracle.txt).

The BER source writer reproduces the OTP ASN.1 options and export layout. The number of unequal top-level source locations dropped from 269 to 255. The first remaining top-level difference is decode_disp/2. The abstract forms and debug options match after removing annotations. The module is 109,896 bytes against 99,008. Code, registers and metadata still differ, including Dbgi, Docs, Line and Type. [Coordinate comparison](coordinates-oracle.txt), [abstract form comparison](all-forms-oracle.txt).

125 of 274 functions still differ after instruction normalization. The largest instruction surpluses are in enc_LDAPMessage_protocolOp/2, enc_SearchRequest/2 and enc_Filter/2. [Instruction gap ranking](instruction-gaps-oracle.txt).

The release oracle is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The writer follows asn1ct_gen.erl and uses the existing Windows release profile for option values and their layout. The original ASN.1 generator was invoked only as a development oracle. No dependency or license changes.

Built the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part650-final-tool` and ran `dotnet artifacts/beam-backend/part650-final-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part650-final.json`. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The full runtime harness and canonical readiness regeneration remain unrun.

All 1,366 rows were checked. The final report shows 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported, with no status changes from Part649. ELDAPv3 remains Different. [Corpus report](otp-corpus.json).

All three focused compiler checks passed. The initial fixture used an unsupported standalone arithmetic body. The final fixture uses a supported tuple-producing helper to test the conversion and forwarding sequence. OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type errors. [Initial focused report](focused-initial.json), [final focused report](focused.json).
