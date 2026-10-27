# Original post-return map observations, part 505

The initial proposal was source-only. Parent-controlled original acquisitions,
managed comparisons and explicit remaining gates are recorded in
`docs/validation/part-505/native-map-materialization/README.md`. Independent
canonical/default goldens are preserved from the actual complete original batch.
Authority is OTP-29.1.1 / `ad05823719d77c8faee87348ea39513d4e2f99c5`.

The exact proposed C/Erlang fixture is copied from batch505. The matrix is
32 cases: map sizes 2/32/33/65, binary sizes 64/65, mutation targets
1/128/129/143. Each case creates a native map, changes one newly allocated
binary key's bytes before returning, then observes the saved map after return:
lookup, traversal, exact comparison, use as an outer HAMT key, and collection.
These are observations of source representation and physical sharing, not
permission to replace the captured map by a normalized logical map.

The collector builds one DLL and acquires every case in its own owned fresh
original VM. A crash, timeout, raised error, bad protocol, or bad module cleanup
fails that case, retains all diagnostics and continues the other independent
cases. The provider's complete 6427-file inventory is checked once before and
once after the batch. No original OTP build or provider copy is added.

Only after all 32 individual cases pass does the collector start one fresh
original VM running actual `map_postreturn505:run/0`. It captures the actual
whole returned list in both serialization profiles. That whole original batch,
never concatenated or reconstructed per-case values, is the comparison target.
Optional independent golden files can be added only from an actual original
batch capture and sealed for a fresh repeat. They are present after the parent-controlled acquisition; resealing never synthesizes them.

The actual observed term is serialized twice inside the same VM:
`returned.etf` uses `[deterministic,{minor_version,2}]`, and
`returned-default.etf` uses `[{minor_version,2}]`. Both files remain untouched.
Both whole batch profiles compare strictly against ordinary and reviewed loader
routes. There are no allowed default-order exclusions and no ETF normalization.
Failed golden comparison does not suppress independent prepared loader routes.

Full mode gates loader execution on native/managed preparation and all 32
original safety successes. Ordinary and reviewed routes run independently;
their failures aggregate. Original-only mode does not require a managed build.
Every original VM writes compile, metadata, outcome, returned profiles, and
module cleanup ETF; caught errors preserve the complete original stack. Native
stderr and crash dumps are retained. The existing owned-process helper closes
streams and reaps processes, with bounded tree termination on timeout.

Parent-controlled source-only seal:

```powershell
py -3 -B tools/Native/Erlang.NifMapMaterialized505Observations/seal-plan.py --repository .
```

The six authored source files use raw-byte hashes. Imported helper, decoder,
and present dedicated backend driver sources use explicit LF text hashes, with
actual raw byte hashes captured separately before imports. Add this new tool
directory to repository `-text` attributes before publication. Reseal after the
parent's dedicated backend driver files are final.

Initial acquisition:

```powershell
py -3 -B tools/Native/Erlang.NifMapMaterialized505Observations/collect.py `
  --repository . `
  --provider-admission artifacts/library376-official-compiler/official-admission.json `
  --plan-sha256 <parent-approved-plan-sha256> `
  --output artifacts/native-map505/original1 --original-only
```

Full comparison uses the same command with a fresh output and without
`--original-only`. `--check-golden` is available only after a parent-acquired
original golden pair is supplied and resealed. No fallback seeds reference
bytes from managed execution.

Backend dispatch is parent-owned:
`--map-materialized505 <admission> <admissionSHA> <routeOutput> <ordinary|reviewed>`.
Admission status is `PARENT_REVIEWED_MAP_MATERIALIZED505_ADMISSION`, build status
is `MAP_MATERIALIZED505_WINDOWS_BUILD_COMPLETE`, and library/module name is
`map_postreturn505`. Backend outputs are `returned.etf`, `returned-default.etf`,
and the same environment/binary/module-retirement completion fields as part504.
No runtime, existing workflow, shared harness, or source OTP file is edited.
