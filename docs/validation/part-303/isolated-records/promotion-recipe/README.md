# Future303 promotion and evidence recipe

This is an artifact-only recipe on the already validated isolated303 candidate. It performs no Main/Git edit, native/original execution, workflow dispatch or further heavy build/full suite. Future302 parent HEAD is deliberately unknown and must be explicitly supplied by the caller; this recipe never guesses or labels the old snapshot as that later head.

## Source and exact parents

promotion-map.json maps the exact eleven formatted/token-checked CSharp files, with existing-file parent SHA or null for absent paths. Five are production files (one existing Execution.cs and four new dormant internal source-caller files); five are HOST definition/constants/coverage files, plus one Program.cs registration. The Program delta is exactly one Test(SourceLoadNifHostNames.Test, SourceLoadNifHostChecks.Run) row after the existing NifSourceEntry row. Parent bytes are actual301 snapshot bytes even though historical isolated snapshot's published baseline was c2d2cc304d03201dd52592c77d0389062aea0268. Actual301 source14/12 freezeA763E55596D771736233F06A197ED46485013D8EFD0D01D13B854BE44AFAED90 and validation freeze54273CB27ECD2E7A26878CD64AD6CBDB59E1BD53CBBFD1C5AAC651CF0A4C2D11 remain distinct provenance.

Future read-only preflight (not executed against Main here):

    python verify-promotion-parents.py --repository <actual-repository> --expected-head <explicit-reviewed-published302-SHA> --output <repository>/artifacts/fresh303-preflight.json

It refuses any changed parent, pre-existing new path, proposed-source drift, wrong/omitted expected HEAD or non-artifact report destination. It does not copy/promote anything. After a successful preflight root may apply the exact source-overlay under its authorized delivery workflow, record actual parent HEAD and rerun required actual Main checks. Do not run the old preflight again after promotion expecting parent matches.

## Native source-plan reseal simulation

The original1003-input plan is preserved byte-for-byte as native-plan301-baseline-1003.json with SHA DCDB739375C0CA2C11EB8DC2998A9B790BD6F202D46FAAB136F13F471544D03D. The existing exact sealer was executed only against candidate, with old candidate plan first preserved then replaced at its canonical self-excluded path. Final simulated plan C8577299D35B72112F8FFAE3BEBD689A8BC9D7D8318F1601EBD75178D6B937E0 contains1007 inputs. Every record path/length/mode/hash was independently checked. Exactly four new production files are added, no input removed, and only existing Execution.cs input record changes. The existing native tools, original13 fixture/collector/guard and C veneer bytes are unchanged.

Only candidate's nif-pending-native.yml REVIEWED_SOURCE_PLAN_SHA256 scalar was replaced once with the new hash. Original oracle13 EAC7... plan literal and all workflow steps remain unchanged. Both proposed plan and workflow are included in source-overlay as the two metadata paths in addition to eleven source files. The simulation writes no .git and launches no SDK/native workload; its native source seal is a prospective build-input closure, not execution evidence. Before future Main promotion/reseal verify the actual source path set is still1007; any legitimate intervening source change requires a separately reviewed plan/hash rather than forcing this plan.

## Actual managed evidence and preserved failures

SourceCaller HOST plus twelve related groups actually passed (13 total) in focus13-revised.log and exit0, including genuine CodeGeneration/Roslyn-loaded source order/failing-second routes, source/BEAM/admission/lifetime/tail/loading/purge and historical BeamLoaderMfaChecks:Races +InitializationLifecycleChecks. Initial/post-format target builds had zero errors/warnings. Eleven-file actual Roslyn Kind/Text token hashes matched72DD52C78AF3370EE9E33C220144DE2CEB8B52C408A22F8806E8010071188012. All2044 unchanged snapshot paths were rehashed.

Retained reproducible preparation failures: project whitespace formatter hit MSBuild named-pipe access denial; first folder attempt rejected --folder with --no-restore; final whitespace --folder with explicit11 paths (without that incompatible flag) plus existing custom SourceLayout succeeded. Initial focus got elevenPASS then hit wrong default method signature; separate focus plan changed only BeamLoaderMfaChecks to BeamLoaderMfaChecks:Races, and full13 rerun passed. No assertion was weakened or production algorithm repaired. validation-result.json in the parent folder binds all actual logs, exits, build assemblies and source hashes.

## Expected scope and future checks

New named HOST adds one row: expected full6622 if all checks pass (not yet executed for303). Registry presence remains503 total public MFAs /345 erlang; no default load_nif handler/BIF, no public MFA or ordinary differential corpus case is added. Existing source capability hook stays explicitly installed/internal. This is not whole load_nif, general native ABI/resources/dirty scheduling, actual new original/native proof or public activation.

Root sequence after future302 is actually published: explicit-head parent preflight; apply13 mapped paths; verify actual sealer path/hash closure and scoped workflow scalar; actual target and managed source/BEAM/admission/lifetime/tail/historical focus; actual full suite; inventory/English changelog/readiness/evidence updates; commit/publish and exact-head mandatory CI/native evidence where required. Never relabel historical301 native receipts as this new source plan's native execution. The current recipe deliberately stops before those future Main/external actions.
