# Native record foundation prototype - 2026-10-13

Isolated archive of a2ca3de2cfaf958ab14f95767e6e3793721a2ff4. Official baseline OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5, read with git show only. Main checkout and sibling were not edited.

## Source-directed decisions and notices

Altered C# adaptations of erts/emulator/beam/erl_record.c and erl_record.h (Ericsson AB 2024-2026), lib/stdlib/src/records.erl (Ericsson AB 2025-2026), erl_bif_op.c (Ericsson AB 1999-2026), external.c, external.h, utils.c and erl_term.h (Ericsson AB 1996-2026), erl_term_hashing.c (Ericsson AB 2022-2026), Apache-2.0. Attribution is consolidated here and existing docs/OTP-PORTS.md; existing third-party full license remains. No source license banners, external package, production BEAM dependency, public package publication, or owner LICENSE.local.txt included.

Existing .NET immutable term, collection, registry, code generation, ETF stream, hashing and display infrastructure was inspected and reused. A framework/tuple/map substitution cannot preserve native-record identity, so a separate NativeRecordTerm is introduced. Arrays are copied and wrapped read-only. Fields/values retain declaration order; atom-index sorting is derived only where the original native hashing/update algorithm needs it. No BEAM record-definition pointer or GC lifecycle is emulated.

## Implemented bounded behavior

NativeRecordTerm carries module/name/export flag/ordered field names/values. Term order is tuple < native record < map. Same-kind comparison follows module, name, exported flag, field count, declaration field names, then declaration values. Exact versus numeric equality and dictionary key hashes preserve existing term semantics. records:create/4 checks module/name before options, exactly one boolean is_exported option, proper-list shape before field inspection, tuple pair shape, atom field names and duplicate rejection. Invalid atom names produce the original {badfield,{{Module,Name},Field}}; invalid module/name produce {badrecord,Name}.

Seven actual records exports: create/4, update/4, get/2, get_module/1, get_name/1, get_field_names/1, is_exported/1. update validates source, identity (including name '_' wildcard ignoring module), map shape and unknown-field errors, and returns the same value for an empty update. get_name/module/get invalid arguments use badarg; get_field_names/is_exported use {badrecord,Value}. Runtime updates preserve declaration order and original snapshot.

erlang:is_record/1 is registered and legal in guards. /2 retains tuple behavior and recognizes native record name. Atom third argument of /3 means module/name native identity; exported status is irrelevant. Numeric third argument remains tuple-only. Native compiler declarations, exported-field access and record_info are outside this foundation; /2 direct literal calls keep the existing tuple-record compiler expansion, while dynamic apply tests exercise the BIF.

ETF RECORD_EXT tag67: uint32 field count, export flag0/1, module atom, name atom, declaration field atoms, declaration values. Encoder/decoder, boundary scanner, size, compressed/safe/local infrastructure and nested terms reuse existing options. Duplicate/nonatom fields, invalid flags and truncated streams are rejected. Native display and node-reference traversal understand record field values.

phash2 definition hash is the original tuple {Module,Name,Exported,DeclaredFields...}; instance mixes field count/definition hash with HCONST_23 and visits atom-index sorted values. Legacy phash visits the definition tuple containing field-order tuple/hash/module/name/exported/sorted keys, then sorted values, and applies FUNNY_NUMBER15 to field count+1. Original atom registration order can differ from the SDK; two new numeric assertions are explicitly SDK-generated expectations pending original proof. A self-equality roundtrip test alone does not establish original hash compatibility.

## Validation and explicit gaps

Initial focused tests18/21 exposed reserved keyword 'maybe' fixture, missing qualification for external_size/phash and an omitted EtfWireBoundary RECORD_EXT branch. Initial19/21 runtime badarg evidence retained. Those new fixture/implementation defects were repaired;21/21 focused compiled cases passed before adding two numeric original-comparison contracts. Final focused/full report is artifacts/native-focused.log and artifacts/native-tests.json; report counts are factual run evidence only. Existing4093 source fixtures,132 diagnostics and six known exclusions are retained, append-only new fixture group. Root must perform immutable corpus audit and source-built original comparison before claiming reference proof.

Unimplemented: native -record declarations, exported native definitions, create/access/update/match grammar, wildcard/access permission rules, get_definition/2, module loader registration/hot code definition canonicalization, native record_info and compiler optimization/lint annotations, definition metadata memory/GC/reduction limits, full stack/error-info/resource contracts, connected distribution/cross-node atom-order hashing and upstream native-record suites. No full record/module/compiler compatibility claim.

## Safe integration

Copy new NativeRecord*.cs, EtfRecordLayout.cs and NativeRecordCases.cs. Merge small hunks in CoreModules, RecordPredicateRegistrations, RecordPredicateSyntax, Semantics, TermOrder, EtfTags/EtfWireBoundary/ExternalTermFormat, ErlangNativeHash/ErlangLegacyHash, TermDisplayWriter, RuntimeKnownNodes, CompiledModuleCases and FunctionalMfaContracts. Do not overwrite main documentation/changelog with this archive. Root owns final checkpoint numbers, readiness, corpus audit, original oracle, publication and provenance. Main/sibling/closed changelog volumes remain untouched by this agent.