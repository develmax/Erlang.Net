Run from the repository root using PowerShell:

    $env:DOTNET_TieredCompilation='0'
    dotnet build docs/validation/part-052/benchmark/Benchmark.csproj -c Release -m:1 -nr:false
    dotnet run --project docs/validation/part-052/benchmark/Benchmark.csproj -c Release --no-build

Output: artifacts/part-052-map-lookup-benchmark.json. Do not overwrite immutable reports.
Five alternating samples of the prior FirstOrDefault scan and current TryGet; prebuilt integer-key maps, 20,000 identical queries, 20% misses, medians/allocations/identical checksums. Construction and retained map memory are excluded. Scoped tier disabling avoids JIT tier transitions midway through sizes. Host-specific measurements, no timing assertions. This is not a BEAM or full-language benchmark.
