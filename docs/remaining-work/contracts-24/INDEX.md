# P0 source-contract inventory after Part530

All24 known MFAs have source-only inventories against pinned OTP29.1.1/ad058237 and baseline mainc7fb01a. These documents do not execute tests or establish compatible contracts.

| Inventory | MFAs | Source units |
| --- | ---: | --- |
| [Inspection/system](p0-inspection/README.md) |4|83selector rows and17default fields|
| [Spawn/proc_lib](p0-spawn-proc/README.md) |19|177named source partitions|
| [load_nif](p0-load-nif/README.md) |1|103named source partitions|

The19spawn/proc_lib entries here differ from the19shared-resource cohort: eight Erlang spawn entries plus all11proc_lib entries, versus12Erlang resource consumers plus seven proc_lib startup entries. Infrastructure's fixed24engineering units also remain distinct from these24MFAs.

Execution remains governed by [the implementation plan](../plan-24-after-530.md). Known24/full180/pending504/registered708 are unchanged. Runtime ordering fixes in Part531 do not close P1 custody/P2atomics or any complete MFA.
