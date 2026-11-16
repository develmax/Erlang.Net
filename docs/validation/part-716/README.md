# Part716 LDAP response decoder frames

The compiler extends required field scheduling to three required fields followed by two or three optional fields. It reuses the consumed tail slots and forwards the last tail through x0. The regression covers known and unknown result codes, missing optional fields and an ignored improper tail.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The compiler reuses the existing enumeration dispatch, decoder calls, optional checks and failure paths. Dependencies and licenses are unchanged.

dec_BindResponse/2 drops from 200 to 198 instructions and from 16 to five stack slots. dec_ExtendedResponse/2 drops from 219 to 216 instructions and from 22 to six slots. Both match OTP after renumbering labels. The managed image is 98,984 bytes and the OTP image is 99,008 bytes. Code and the Attr vsn differ. [Instruction comparison](response-delta.txt), [sections](chunks-oracle.txt), [remaining instruction sites](code-sites-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part716-tool`. Ran its DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part716.json. Full runtime validation and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report.

Three focused checks passed. The corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. The LDAP behavior oracles matched 320 direct calls, 83 round trips and 88 boundary calls. [Focused checks](focused.json), [corpus](otp-corpus.json).
