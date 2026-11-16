# Part688 LDAP constructed BER part collection

The C# emitter recognizes the part collection clause family, preserving nested list recursion, the byte and binary tail pattern, the plain accumulator path and reversal before list_to_binary/1. It emits a two-slot frame only on the nested call path and an empty frame for the final conversion. The bit and plain tail calls remain frameless. The fixture covers a nested list, a bit payload and an empty binary that falls through to plain collection.

All 41 instructions in collect_parts/2 match OTP 29.1.1 after renumbering labels. The previous managed function used 78 instructions and a 20-slot frame. The exact encode/2 and decode/2 comparisons and the normalized search decoder and tag matcher comparisons still pass. [Part collection comparison](collect-parts-normalized-oracle.txt), [disassembly](collect-parts-oracle.txt).

The managed image drops from 102,384 bytes and 8,060 instructions to 102,224 bytes and 8,023 instructions. OTP has 99,008 bytes and 7,028 instructions. AtU8, ImpT, StrT, CInf, Meta, LitT, Docs and Dbgi remain byte-exact. Code, ExpT, LocT, Attr, Line and Type differ. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The matcher follows the constructed BER collection clauses in asn1rtt_ber.erl. The C# emitter reuses tuple and list tests and the existing BEAM binary match commands. Dependencies and licenses are unchanged.

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part688-tool`. Run `dotnet artifacts/beam-backend/part688-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part688.json` with InstalledOtpRoot from artifacts/library376-official-compiler/official-admission.json. The focused selection is compiler/beam/whole-module-matches-otp, compiler/beam/rebuild-installed-otp-release-images and compiler/beam/documentation-payload-matches-otp.

The behavior oracle matched 320 direct calls, 83 round trips and 88 boundary calls. The full runtime harness and canonical readiness regeneration remain pending. The largest remaining instruction gaps include encode_length/1 and collect_parts_bit/3. [Instruction gaps](instruction-gaps-oracle.txt).

The initial fixture run did not admit the lists:reverse/1 registry dependency. Declaring the lists dependency in the fixture resolved it and all three focused checks passed. The full corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. The corpus exited with code 1 because images still differ. [Initial focused report](focused-initial.json), [final focused report](focused.json), [corpus report](otp-corpus.json).
