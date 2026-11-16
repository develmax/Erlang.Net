# Part689 LDAP constructed BER bit part collection

The C# emitter recognizes the bit part collector and keeps the accumulator, tail and binary fragments in X registers through matching and recursion. The extracted unused-bit count has an unsigned byte type. The accumulated count is annotated as a nonnegative integer only for a private function when every observed direct call enters from the validated byte-extraction path or the validated recursive addition and no local function reference escapes. The final reverse call saves only the count. The fixture checks the sum and the output byte order across two fragments.

All 29 instructions in collect_parts_bit/3 match OTP 29.1.1 after renumbering labels. The previous managed function used 53 instructions. The normalized comparisons for collect_parts/2, match_tags/2 and dec_SearchRequest/2 and the exact encode/2 and decode/2 comparisons still pass. [Bit collector comparison](collect-bit-normalized-oracle.txt), [disassembly](collect-bit-oracle.txt).

The managed image drops from 102,224 bytes and 8,023 instructions to 102,148 bytes and 7,999 instructions. OTP has 99,008 bytes and 7,028 instructions. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi remain byte-exact. Code, ExpT, LocT, Attr, Line and Type differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The C# path reuses the byte and tail match emitter, type table writer and tail-call opcodes. Dependencies and licenses are unchanged.

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part689-tool`. Run `dotnet artifacts/beam-backend/part689-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part689.json` with InstalledOtpRoot from artifacts/library376-official-compiler/official-admission.json. The focused selection is compiler/beam/whole-module-matches-otp, compiler/beam/rebuild-installed-otp-release-images and compiler/beam/documentation-payload-matches-otp.

The behavior oracle matched 320 direct calls, 83 round trips and 88 boundary calls. The full runtime harness and canonical readiness regeneration remain pending. Next, align encode_length/1 and the remaining instructions and metadata. [Instruction gaps](instruction-gaps-oracle.txt).

All three focused checks passed. The full corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. The corpus exited with code 1 because images still differ. [Focused report](focused.json), [corpus report](otp-corpus.json).
