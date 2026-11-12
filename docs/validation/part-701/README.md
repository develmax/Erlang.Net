# Part701 LDAP list comprehension binary conversion

The parser preserves the source coordinate of the list generator operator. The emitter uses that coordinate for the generated helper and its recursive call when a binary-to-list conversion follows the local reader. It reuses the existing one-slot list mapping loop and still checks whether a local function shadows binary_to_list/1. The fixture covers mapping order, empty input and an improper tail.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The generator annotation comes from erl_parse.yrl and v3_core.erl in the pinned checkout. The emitted instruction sequence was verified against the installed release. Dependencies and licenses are unchanged.

Four helpers each drop from 38 instructions to 25 and match OTP after renumbering labels. The managed image drops from 101,320 bytes and 7,716 instructions to 101,164 bytes and 7,664 instructions. OTP has 99,008 bytes and 7,028 instructions. Code, ExpT, LocT, Attr, Line and Type still differ. [Helper comparison](comprehension-delta.txt), [sections](chunks-oracle.txt), [instructions](disassembly-oracle.txt).

The Line header and all 856 source locations now match. The order still differs at 33 positions, down from 37. The first three differing positions come from enc_PartialAttribute_vals_components/3, where OTP emits the nonempty branch before the empty branch. [Header](line-oracle.txt), [order](line-order-oracle.txt).

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part701-tool` and ran `dotnet artifacts/beam-backend/part701-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part701.json` using the release root from artifacts/library376-official-compiler/official-admission.json. The behavior oracles matched 320 direct calls, 83 round trips and 88 boundary calls.

All three focused checks passed from an isolated build directory. The prior full harness still held assemblies in the default build directory. The fresh full runtime harness and canonical readiness regeneration remain pending. The immutable Part561 comparison report remains selected.

The corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 is Different. [Focused report](focused.json), [corpus report](otp-corpus.json), [first remaining sequence encoder difference](sequence-encoder-oracle.txt).
