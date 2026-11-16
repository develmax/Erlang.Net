# Part707 LDAP list result instructions and Type parity

The compiler lowers the guarded key lookup in info/0 without stack slots and annotates the result of lists:keyfind/3 as an atom-or-tuple union. It keeps the selected octet list in x0 while length/1 writes to x1 in encode_integer/1. The integer operand annotation requires integer-guarded callers and the cons annotation requires proven nonempty accumulator returns. The binary-guarded byte_size operand records byte alignment in the wire type table. The fixture covers key lookup and both sign branches, and the existing measured binary fixture exercises byte alignment.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The unit flag and the unit-minus-one representation follow the pinned beam_types.erl. Dependencies and licenses are unchanged.

info/0 drops from 35 to 24 instructions. encode_integer/1 drops from 32 to 21. Both match OTP after renumbering labels. Type and Line match byte for byte. The managed image is 99,236 bytes with 7,063 instructions. OTP has 99,008 bytes and 7,028 instructions. [Instruction comparison](list-results-delta.txt), [sections](chunks-oracle.txt), [wire types](type-wire-oracle.txt).

Code, ExpT, LocT and Attr differ. ExpT and LocT have the same function keys in the same order and differ only in entry labels. Attr differs only in vsn. [Function tables](function-tables-oracle.txt), [attributes](attr-oracle.txt), [remaining instructions](instruction-gaps-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part707-final-tool`. Ran its DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part707-final.json. The LDAP behavior oracles matched 320 direct calls, 83 round trips and 88 boundary calls. Full runtime validation and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report.

The first fixture used a two-element open list in the test helper, outside its current simple emission path. The corrected fixture exercises the sign branches with supported list construction. All three focused checks passed on the final sources. The corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. [Focused checks](focused.json), [corpus](otp-corpus.json).
