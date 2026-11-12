# Part706 singleton list dispatch

The emitter unwraps a singleton list with a local tail call, returns the original input when the identity guard succeeds, and tail-calls the external fallback otherwise. Both tail calls are frameless. The fixture exercises nested singleton lists, a binary value, an ordinary list and an empty list.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The compiler reuses its existing list checks, identity guard matching and tail-call emission. Dependencies and licenses are unchanged.

All 15 instructions in ber_encode/1 match OTP after renumbering labels, down from 27. The managed image is 99,292 bytes and contains 7,085 instructions. OTP has 99,008 bytes and 7,028 instructions. Line still matches byte for byte. Code, ExpT, LocT, Attr and Type differ. [Instruction comparison](ber-dispatch-delta.txt), [sections](chunks-oracle.txt).

The decoded Type table has 15 OTP entries and 12 managed entries. The two missing decoded type shapes are the atom-or-tuple union used in info/0 and the cons type used by the length instruction in encode_integer/1. [Type table](type-oracle.txt), [operand sites](type-sites-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part706-tool`. Ran its DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part706.json. Full runtime validation and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report.

Three focused checks passed. The corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. The LDAP behavior oracles matched 320 direct calls, 83 round trips and 88 boundary calls. [Focused checks](focused.json), [corpus](otp-corpus.json).
