# Ordsets term comparison contracts

Pinned baseline: OTP29.1.1 / ad05823719d77c8faee87348ea39513d4e2f99c5.

The unchanged compiled pool/0 constructs190 adversarial terms inside each VM.
Only the scalar matrix of36100 ordered pairs crosses ETF. Each pair checks
term ordering, numeric equality and exact equality in a real actor/runtime scope.
Ten additional guest probes compare the actual ordsets equality, membership and
sortedness calls with the complete scalar outcomes produced by original OTP.
Six disjoint32-first-term shards cover the entire matrix; there is no sampling.
Both managed and original BEAM ordsets/list selections are explicit. Original
images are hash checked and all original ordsets exports must retain BEAM ownership.

```powershell
py -3 tools/Validation/capture-ordsets-term-contracts.py --check
dotnet build tools/Validation/Erlang.TermComparisonContracts/Erlang.TermComparisonContracts.csproj -m:1
# Run each SHARD from0 through5 with both switches:
dotnet run --project tools/Validation/Erlang.TermComparisonContracts --no-build -- --runtime 2
dotnet run --project tools/Validation/Erlang.TermComparisonContracts --no-build -- --beam-runtime 2
```

A RuntimeSnapshotDirectory MSBuild property can instead bind the tool to a saved
set of six runtime/library binaries. Record those binaries and source identities
with the reports; never substitute a stale test-bin copy for the primary outputs.

Source includes local unloaded fun code identities and environments, external
functions, signed zero and large numeric boundaries, Unicode atoms, tuples,
improper/deep lists, bitstrings, PID/port/reference wire identities, native records,
nested map keys and the flat/HAMT boundary. Tied distinct fun keys stay in the
fixture. For those keys, bulk OTP sorting depends on native addresses, so the
permanent large-map witnesses use deterministic incremental collision history.
The retained original repeated diagnostic demonstrates this distinction.
Deterministic ETF round-tripping of arbitrary pathological maps is deliberately
not used to normalize the inputs or to create expected values.

This is a finite differential witness, not a universal module acceptance claim.
Dependent lists helper audit, native ownership regression, final merge/source
binding checks and other platform acceptance remain separate gates.
