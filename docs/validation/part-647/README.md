# Part647 LDAP binary decoding and atom table parity

The C# emitter recognizes signed decoding where a local call yields the input, byte_size measures it and a signed unit-eight match consumes it. Successful byte_size proves a bitstring, allowing the OTP bs_start_match4 no_fail path. The emitter retains the original input for badmatch and emits the inferred operand types. The managed regression covers signed output, a partial-byte badmatch and a non-bitstring badarg.

Single-byte wildcard patterns use an exact-length test. Single-byte literals use integer extraction and an exact test, avoiding the extra skip and equality command atoms in LDAP. Multi-byte patterns retain their existing handling.

AtU8 and ImpT now match the OTP release byte for byte, including all 352 atoms and 19 imports. decode_integer/2 matches all 18 instructions after normalizing source lines and local label numbers. The complete generated module contains 10,654 instructions against 7,028 in OTP. The file remains Different at 110,084 bytes against 99,008. Code, remaining register choices and source/type metadata still differ. [Chunk comparison](chunks-oracle.txt), [signed decoder comparison](signed-oracle.txt), [table order](order-oracle.txt).

Both builds and three focused checks passed. OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type reasons. The 300 abstract forms still match after removing coordinates. Reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The work follows the pinned binary instruction forms and reuses the C# emitter. No dependency or license changes.

The corpus tool was built with dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part647-tool. It ran as dotnet artifacts/beam-backend/part647-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part647.json. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The full harness and canonical readiness regeneration were not run. No runtime MFA promotion or publication.

The full corpus recorded 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported out of 1,366. [Corpus report](otp-corpus.json), [focused tests](focused.json).
