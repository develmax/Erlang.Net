# Part687 LDAP tag matching

The C# emitter recognizes the tag-matching clause family from its source patterns and calls. It checks the tuple, singleton list and list head directly in X registers, preserves the original values on the mismatch paths and emits frameless tail recursion. The final wrong_tag branch builds the same nested reason and tail-calls exit/1. A shadowed exit/1 or a different clause shape uses the general emitter. The fixture covers tuple and wrapped recursion, list return, empty tags, wrong_tag and function_clause.

All 56 instructions in match_tags/2 match OTP 29.1.1 after renumbering labels. The previous managed function used 98 instructions and 32 stack slots. The new function has no frame. The exact encode/2 and decode/2 comparisons and normalized search and protocol operation decoder comparisons still pass. [Tag matcher comparison](match-tags-normalized-oracle.txt), [disassembly](match-tags-oracle.txt).

The managed image drops from 102,608 bytes and 8,102 instructions to 102,384 bytes and 8,060 instructions. OTP has 99,008 bytes and 7,028 instructions. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi remain byte-exact. Code, ExpT, LocT, Attr, Line and Type differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The C# path reuses the existing tuple and list tests, register equality and tail-call emission. Dependencies and licenses are unchanged.

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part687-tool`. Run `dotnet artifacts/beam-backend/part687-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part687.json` with InstalledOtpRoot from artifacts/library376-official-compiler/official-admission.json. The focused selection is compiler/beam/whole-module-matches-otp, compiler/beam/rebuild-installed-otp-release-images and compiler/beam/documentation-payload-matches-otp.

The behavior oracle matched 320 direct calls, 83 round trips and 88 boundary calls. The full runtime harness and canonical readiness regeneration remain pending. Next, align collect_parts/2 and the remaining label and metadata encoding. [Instruction gaps](instruction-gaps-oracle.txt).

The initial fixture build had a missing parenthesis in the expected wrong_tag value. The corrected fixture passed all three focused checks. The full corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. The corpus exited with code 1 because images still differ. [Focused report](focused.json), [corpus report](otp-corpus.json).
