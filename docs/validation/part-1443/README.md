# Part 1443 — Select the record update hint

The general record update emitter selects reuse for an effective single field when it cannot prove that the old and replacement values differ. It keeps copy for multiple fields, known unequal replacements, and arities outside the OTP conversion range. For a syntactically known record source, it uses the shape proof and drops writes to fields already known to hold the replacement. Operand and source evaluation order is preserved.

The fixture compares reuse and copy instructions against pinned OTP. It checks original record preservation, equal and unequal replacements, two updated fields, removal of a proven no-op, and badrecord. The .NET regression checks that aliasing preserves both the original and the updated tuple.

The implementation reuses exact Erlang term equality and the existing record constructor and BEAM operand writers. The reference hint selection is in beam_ssa_type:simplify_update_record and its update_tuple conversion. General inferred field types and destructive update analysis remain separate differences.

- [Fixture instructions and execution](probe.txt), [focused .NET tests](focused.json), and [compiler hashes](compiler-hash.txt)

The full Windows OTP 29.1.1 run generated all 1,366 release images. It reports 382 Equal, including ELDAPv3 from the C# ASN.1 generator, and 984 Different. Both runtimes accepted all 984 differing images. The change reaches 427 images and 2,959 functions. The aggregate absolute Code size gap grew by 301 bytes. Whole-image equality and normalized function equality did not change.

The scan no longer finds a copy instruction with a matching OTP reuse deficit by arity and field indices. It previously found 2,175 functions in 389 modules. The inverse difference now appears in 856 functions across 258 images. In asn1ct:scan_pass/1 the two hints agree with OTP, and redundant record checks remain.

A separate scan found 3,839 functions in 469 images with more explicit badrecord checks for a record shape than corresponding OTP tag tests. The scan compares counts within a function and does not establish the control-flow proof for any individual check.

- [Corpus](otp-corpus.json), [delta](summary.txt), [release instructions](release-records.txt), [reuse deficit](reuse-deficit-after.txt), [copy deficit](copy-deficit-after.txt), and [record check scan](record-checks-after.txt)
