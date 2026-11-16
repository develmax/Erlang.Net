# Part721 LDAP terminal required decoder fields

The compiler schedules a terminal required field after a bounded or enumerated first field. It preserves the required failure labels, moves the first result before trimming the two-slot frame and preserves the original range and badmatch terms. The fixture covers an ignored improper tail, a range exit and a failed final match.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The compiler reuses its list split, enumeration, range and frame instructions. Dependencies and licenses are unchanged.

dec_BindRequest/2, dec_ModifyRequest_changes_SEQOF/2 and dec_RealSearchControlValue/2 each gain one instruction and match OTP disassembly including labels. Six functions retain instruction differences. The managed image is 98,996 bytes and the OTP image is 99,008 bytes. Code and the Attr vsn differ. [Instruction comparison](terminal-required-delta.txt), [sections](chunks-oracle.txt), [remaining instruction sites](code-sites-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part721-tool`. Ran its DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part721.json. Full runtime validation and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report.

Three focused checks passed. The corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. The LDAP behavior oracles matched 320 direct calls, 83 round trips and 88 boundary calls. [Focused checks](focused.json), [corpus](otp-corpus.json).
