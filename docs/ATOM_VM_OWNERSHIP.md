# Atom tables and logical VM ownership

Each `ProcessRuntime` owns its atom-name table, registration ordinals and UTF-8 atom-name binary cache. Immutable atom values compare by name and can be passed between runtimes; receiving a term admits its atom names to the destination table. The tables do not share capacity or registration order.

```csharp
await using var vm = new ProcessRuntime(new ProcessRuntimeOptions
{
    MaximumAtoms = 16384
});

using (vm.EnterAtomScope())
{
    var name = Term.A("my_application");
    // Host-side construction/decoding here belongs to vm, including across await.
}
```

The default capacity is 1,048,576; the minimum configurable capacity is 8192. `AtomCount`, `AtomLimit` and `erlang:system_info(atom_count | atom_limit)` inspect the logical VM. Runtime process execution, module calls, module registration, BEAM preparation, runtime ETF imports and bound NIF environments establish the appropriate table automatically. An explicit scope is useful for host-side term construction. Scopes must be disposed in reverse nesting order. A host that constructs standalone terms without a runtime scope uses the separate host registry; importing such values admits them to the destination VM.

Adding a fresh name to a full VM table requests immediate logical-VM halt with status1. Erlang catch handlers and Erlang `after` clauses do not run for this transfer; CLR cleanup still unwinds. `HaltCompletion` reports the selected result, while `HaltQuiescence` additionally waits for process/resource retirement. Neither path exits the .NET host. Other `ProcessRuntime` instances remain operational.

Invalid names and overlength conversion inputs retain their Erlang argument/system-limit errors; they are separate from exhausting the table with valid fresh names. Existing names do not consume another slot. Standalone host-registry exhaustion retains its existing host API behavior and does not select an arbitrary logical VM to stop.

The table and UTF-8 cache are reclaimed with their owner when no longer reachable; the cache uses an ephemeron keyed by the table. Observed payload diagnostics also use weak keys. Static common immutable atom objects remain reusable, but every cached-name lookup observes the active table.

Acceptance is tracked in Part494. This ownership decision does not by itself close all native ABI, atom-count/bootstrap or module-loading contracts.
