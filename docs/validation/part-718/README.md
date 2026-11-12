# Part718 LDAP two-field optional encoders

The compiler projects the optional input into a stack slot before the first call. It compares the saved value in place, clears that slot when invoking the optional encoder, and builds the payload list from the two result pairs. The regression exercises a supplied optional value and the default branch.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The compiler reuses its two-field schedule, optional sentinel dispatch and pair-length analysis. Dependencies and licenses are unchanged.

enc_SaslCredentials/2 and enc_ExtendedRequest/2 have the same 40 disassembled instructions and label numbers as OTP. Fourteen functions retain instruction differences. The managed image is 98,984 bytes and the OTP image is 99,008 bytes. Code and the Attr vsn differ. [Instruction comparison](optional-encoder-delta.txt), [sections](chunks-oracle.txt), [remaining instruction sites](code-sites-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part718-tool`. Ran its DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part718.json. Full runtime validation and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report.

Three focused checks passed. The corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. The LDAP behavior oracles matched 320 direct calls, 83 round trips and 88 boundary calls. [Focused checks](focused.json), [corpus](otp-corpus.json).
