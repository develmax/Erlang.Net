# Bounded typed-root and producer-DAG review1593

Exact head7cd478612f702abc80d3a4405ee0471ddc14722a versusb073ced4eb6298405fd4b5fe2902ab206becef56. Read-only explicit Git object inspection. No tests, VM probes, network, full runs, shared source edits, Git mutations or formal approval. No1592 postimage was applied.

## Actual correction verified

Independently verified all11 workflow PlanPins against actual raw plan Git bytes,91 explicitly Root-qualified nested Inputs, and8 present producer-reference edges. This used49 focused source/plan blobs; bindings.json records exact keys, roots, resolved Git paths, modes, hashes and producer values. It does not claim execution of the reported20 controls,20,404-row broader audit or13 passive gates.

All three ENV .gitattributes rows now bind Packet root `tools/Native/Erlang.NifUpgradeObservations/environments/.gitattributes`, SHA256705FD4D6451A31D36B3DF7DE96F83F30AC976C9B4A6D1E51671D8E2F33E2D0DA. This is the packet file, not repository .gitattributes. source_bindings.py:14-19 selects Packet/Tool owner versus Repository explicitly, rejects unknown roots, enforces containment and does not normalize one root into another. TextLF applies only when explicitly declared; the corrected attribute rows bind raw bytes.

N2 original and managed WrongEntryNativeBuildPlans match actual producers separately: Windows DC8F675F473298C1D48379B60ED13C150052BF11021B7CE3EA818F492BD1AFAC from NifVersion651 build-inputs, Linux58CDC4D874E84717E275B7A33FA31F05A9D439ABCCC02CC36956FAA4FCF439BE from the NATIVE_PLAN fixture-host plan. The N2 and ENV original/managed native and original producer references match their corrected current plans. In particular ENV native SHA7559... is valid again for its corrected actual packet-root plan bytes; it must not be called stale merely because that same literal previously pointed at differently hashed bytes.

The collector is byte-identical to b073; src, tests, tools/Build and Differential Git diffs are empty. Actual C#/Erlang/native test bodies are unchanged. Twelve added old-plan snapshots independently match their content-addressed SHA filenames (2,351,636 bytes total). The retained RED41723631184 ZIP is20,981,714 bytes and matches its declared receipt SHA/length. scope.json preserves exact source and evidence bindings. This is preservation of failed evidence, not a green result.

## P2: mandatory producer-reference presence is not enforced

source_bindings.py:63-70 validates NativeBuildPlanSha256 and OriginalPlanSha256 only if each key is present. Actual consumer code requires them by direct indexing: N2 collect-original.py:13/15, ENV collect-original.py:13/15, ENV collect-managed.py:48/56; N2 managed also requires OriginalPlanSha256 at collect-managed.py:83.

Source-level counterexample: remove NativeBuildPlanSha256 from a N2/ENV original role, then consistently reseal that plan, Integrated PlanPins and workflow pin. This new static validator skips the missing field and can pass; the real collector subsequently fails with missing-key rather than a bounded source admission refusal. Similarly remove OriginalPlanSha256 from a managed role. This is a permanent guard omission, not an existing broken edge in exact7cd: every current role field was present and independently verified above.

Recommended bounded correction: explicitly require NativeBuildPlanSha256 for N2/ENV ORIGINAL and MANAGED roles, and OriginalPlanSha256 for their MANAGED roles, then compare to their actual producer plan. Keep native-builder roles exempt from producer fields they do not consume. WrongEntryNativeBuildPlans already uses a comparison that rejects missing data. Add absent-role-field negative controls without constructing substitute runtime fixtures.

No other blocking defect was found in the requested current binding/DAG correction. Hosted acceptance is separate. The1592 blocked component must be rebased to this corrected owner baseline (or a later presence-guard successor) before integration, with typed roots retained.