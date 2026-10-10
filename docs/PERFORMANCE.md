# Performance evidence and acceptance policy

Use C#/.NET facilities when they preserve observable Erlang behavior. Literal source translation is not required. Source algorithms establish the contract; storage, generated code and execution strategies may differ. The intended outcome is a compatible language with better performance on demonstrated workloads, not a promise that every program will beat OTP.

## Current measured optimization

Part052 retains MapTerm's privately owned exact-key Dictionary for Get/TryGet instead of discarding it and scanning sorted entries. Sorted immutable entries remain the ordering/printing/hash/serialization contract. Map construction still copies input and sorts entries. The retained dictionary consumes additional memory for the map's lifetime; this cost has not been measured.

[Saved samples](validation/part-052/map-lookup-benchmark.json) and [reproduction source](validation/part-052/benchmark/README.md) compare the previous C# scan and current indexed lookup using the same prebuilt integer-key maps, cycling queries with 20% misses, 20,000 queries per sample and five alternating samples. Release, .NET 10.0.11, Windows 10, x64, tiered compilation disabled to keep JIT tier consistent across sizes. Allocation bytes and matching checksums are saved. No timing thresholds are used in correctness tests.

| Map keys | Previous scan median ms | Indexed median ms |
| --- | ---: | ---: |
|8|5.05|1.22|
|64|15.30|0.94|
|1024|174.35|0.85|
|4096|896.76|1.05|

These are host-specific lookup measurements. They exclude construction, retained index memory, serialization and end-to-end program execution. They do not compare with OTP or establish superiority to BEAM. Other key shapes, collision distributions, map sizes and runtime configurations require their own measurements. Selected correctness tests check distinct integer/float and signed-zero keys, compound keys, input ownership and immutable entry exposure.

## Cross-runtime comparison requirements

Use the pinned OTP-29.1.1 source commit and a stated .NET SDK/runtime/Release configuration. Compare the same source program, input values, output/error contracts and number of operations. Separate interpreted expression, compiled module and hybrid paths; generated C# currently still evaluates ASTs and is not a complete optimizing Core compiler.

Measure warmed steady state separately from cold compilation/startup. Record hardware/OS, runtime flags, independent repeated samples and distributions. Report throughput/latency, transient allocations, GC and retained memory where relevant. Include small/large inputs, failures, structural keys and concurrency when the workload uses them. A local C# microbenchmark or selected differential pass cannot stand in for this evidence.

Before adopting an optimization, preserve baseline fixtures and add relevant binding, effect ordering, exact equality, immutability and error checks. Document the source decision, resource tradeoff and measurement limits. Keep unverified or regressing results visible. Full grammar/runtime/OTP/reference suites and cross-runtime performance comparisons remain unfinished.
