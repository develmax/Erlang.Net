# Part 1435 — Group register case arms by tuple arity

The register case emitter groups a leading run of tagged tuple arms when it contains more than one arity. It emits one tuple test and select_tuple_arity table. The order of clauses within each arity stays as written. Rejection from the last arm in an arity group goes directly to the first ungrouped clause or the original case failure.

The OTP oracle checks the arity table, repeated tags, source priority, literal and variable fallback clauses, and the case_clause payload. In compile_proc/3 both compilers now emit the tuple arity dispatch. The C# generator still emits separate tag tests within the two-field group and retains six frame slots, compared with three in OTP.

- [Oracle](check-case-arity.ps1), [before](previous-probe.txt), [after](probe.txt), and [compile_proc instructions](compile-proc.txt)

The Windows OTP 29.1.1 corpus generated all 1,366 release images. The strict comparison reports 382 Equal, including ELDAPv3, and 984 Different. Both runtimes accepted all 984 differing images. The change reaches 122 images and 205 functions. The number of functions that emit fewer arity selections than OTP fell from 2,050 to 1,872. The normalized instruction comparison recorded no equality transitions. The aggregate absolute Code section size gap grew by 6,741 bytes in the changed images. Separate tag tests still emit a tag extraction and comparison for each arm.

- [Corpus](otp-corpus.json), [frame and Code size delta](summary.txt), [normalized instruction comparison](normalized-functions.txt), [arity ranking](case-arity-after.txt), and [focused tests](focused.json)
