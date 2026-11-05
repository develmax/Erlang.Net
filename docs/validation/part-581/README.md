# Part581 — PR110 source admission repair

PR110 head `4b71393e9895f2e213b32910cfb636f8afb87d6d` failed in
[local-regressions, run41826409803](https://github.com/develmax/Erlang.Net/actions/runs/41826409803/job/126562146820)
before the runtime build. The Resource529 admission found six stale source
identities, four of thirteen focused plans failed, and the MFA admission found
five stale raw source identities. These are source admission failures; they do
not establish a runtime regression or a passing runtime comparison.

Refresh the declared current source hashes, dependent producer pins and their
existing workflow hash parameters. Preserve historical frozen snapshots,
classifications, native execution states and required checks. The four broad
source plans also admit the newly added file source files in their existing
source scopes. They include previously omitted merged Unicode files in those
same scopes. The actual Pending reviewed-input enumerator independently checks
exact membership; no missing file is excused by a matching plan hash.

`source-plan-refresh/membership-audit.json` lists every added source row and its
scope. `source-plan-refresh/refresh.py` and its audit record identity/pin updates;
they do not run a native build or change acceptance statuses. The raw before
and after reports are retained under `checks/`.

Focused validation: Resource529 passive source admission, all thirteen passive
source plans, all5400 MFA/native raw evidence rows, exact Pending membership,
stale-hash and old-membership rejection controls,22 CI reuse unit tests, and
documentation layout. No product source changes, SDK build, product VM, native
execution, manual CI dispatch or full local regression are needed for this
repair. Hosted CI must still pass on the new PR head.

The [Part580 file results](../part-580/README.md) retain their original scope:
408 exact focused observations per backend and five failing complete filesystem
cycles per backend. This repair promotes no compatibility/readiness status and
does not close the missing prim_file, prim_buffer or RAM-provider contracts.
