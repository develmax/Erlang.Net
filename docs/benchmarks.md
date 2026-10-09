# Exploratory measurements

Measured 2026-10-09 with SDK 10.0.400, .NET runtime 10.0.11, Windows 10 build 19045, x64 and 16 logical processors. Release build, one warm-up plus one measured run per workload; total process allocations include async worker activity. Exact report: `validation/benchmarks.json`.

| Workload | Work units | Elapsed | Allocated bytes |
|---|---:|---:|---:|
| Spawn, send, receive and complete | 10,000 process lifecycles | 95.29 ms | 23,055,808 |
| Load mailbox and select a late matching message | 10,000 queued messages, one selective receive | 0.84 ms | 1,680,392 |
| ETF encode/decode and exact-equality validation | 10,000 terms containing a 100-element list | 154.23 ms | 113,120,040 |

The mailbox result includes loading and scanning the mailbox. It is not the throughput of 10,000 receives. Allocation totals are not retained per-process memory. These are initial single-machine workload observations, not statistically rigorous benchmarks, latency percentiles, CPU/GC profiles or comparisons with BEAM. Spawn memory retention, message latency/throughput, supervisors, timers, binaries and distribution require separate measurements.

```powershell
dotnet build benchmarks/Erlang.Benchmarks -c Release -m:1
dotnet run --project benchmarks/Erlang.Benchmarks -c Release --no-build -- artifacts/benchmarks.json
```
