# Part722 LDAP search decoder failure labels

The planning pass records the number of list-match failure blocks. The emitter assigns labels from the end of the planned function range for those blocks and the range exits, while inline choices keep their forward label sequence. The change applies to the existing reserved-slot decoder schedule. The behavior oracles cover the search decoder and its boundary inputs.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The compiler reuses the existing decoder planning pass and failure instructions. Dependencies and licenses are unchanged.

All 134 instructions in dec_SearchRequest/2 match the OTP disassembly including label numbers. Five functions retain instruction differences. The managed image remains 98,996 bytes and the OTP image is 99,008 bytes. Code and the Attr vsn differ. [Instruction comparison](search-labels-delta.txt), [sections](chunks-oracle.txt), [remaining instruction sites](code-sites-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part722-tool`. Ran its DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part722.json. Full runtime validation and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report.

Three focused checks passed. The corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. The LDAP behavior oracles matched 320 direct calls, 83 round trips and 88 boundary calls. [Focused checks](focused.json), [corpus](otp-corpus.json).
