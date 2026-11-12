# Part661 LDAP arithmetic types and decoder layout

The C# emitter infers nonnegative integer facts for pair members through local calls, branch results and arithmetic. Recursive call contexts are solved to a fixed point. Unknown facts retain untyped operands. Local tail calls in the register-only suffix omit the call-site line as in OTP beam_ssa_codegen.erl. The C# ASN.1 writer restores decoder field separators and multiline octet and enum expressions.

All 155 instructions of enc_SearchRequest/2 match OTP 29.1.1 after normalizing local labels and source lines. The seven additions also match with their exact register operands, live counts and nonnegative integer types. All relative line offsets match. The absolute function start advances from 1097 to 1314 against 1635 in the release. [Arithmetic and absolute lines](search-arithmetic-oracle.txt), [relative lines](search-calls-oracle.txt), [normalized instructions](choice-encoder-oracle.txt).

The generated module is 108,020 bytes against 99,008. AtU8, ImpT, StrT, CInf and Meta remain byte-exact. Code and eight other sections still differ. Dbgi is 54,208 bytes against 54,262 and Line is 2,117 against 2,155. Equal or close chunk sizes do not establish byte equality. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. This change reuses the C# emitter, typed operands and ASN.1 writer. No dependency or license changes.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part661-final-tool` and ran `dotnet artifacts/beam-backend/part661-final-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part661-final.json`. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The full runtime harness and canonical readiness regeneration remain unrun.

All three focused checks passed. OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type errors. The full corpus checked 1,366 modules with 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 remains Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
