# Part717 LDAP choice branch labels

The compiler allocates labels in emission order when every arm of a terminal choice uses the direct tagged decoder path. A range failure receives the label immediately after its arm, and the fallback receives the final label. Mixed and nonterminal choices retain their existing scheduling. The existing singleton decoder fixture exercises the direct arms, the fallback and range failures.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The compiler reuses the existing choice, range and fallback emitters. Dependencies and licenses are unchanged.

dec_AuthenticationChoice/2, dec_LDAPMessage_protocolOp/2, dec_Filter/2 and dec_SubstringFilter_substrings_SEQOF/2 now have the same disassembled instructions and label numbers as OTP. Sixteen functions retain instruction differences. The managed image is 98,984 bytes and the OTP image is 99,008 bytes. Code and the Attr vsn differ. [Instruction comparison](choice-labels-delta.txt), [sections](chunks-oracle.txt), [remaining instruction sites](code-sites-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part717-tool`. Ran its DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part717.json. Full runtime validation and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report.

Three focused checks passed. The corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. The LDAP behavior oracles matched 320 direct calls, 83 round trips and 88 boundary calls. [Focused checks](focused.json), [corpus](otp-corpus.json).
