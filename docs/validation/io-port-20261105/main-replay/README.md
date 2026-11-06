# io replay on main

Base commit `498e10d6265f364e65579848d9fdff1bd949a351`. Built the focused validator with
`dotnet build tools/Validation/Erlang.IoCompatibility/Erlang.IoCompatibility.csproj -m:1 -v:minimal`
with zero warnings and errors. The source audit confirmed that the retained
OTP 29.1.1 source is byte-identical and the explicit inventory contains 53 exports.

The validator replayed the preserved official OTP corpora and verified the
corpus and fixture identities before execution. Managed and Beam each passed
666 of 666 observations at depths 0, 1, 2, 3, 8 and 32. The twelve reports in
this directory record 7,992 comparisons and the runtime and library hashes
from this build.

The full regression suite was outside this focused replay. Full terminal,
filesystem, distributed-peer, platform and release startup coverage remains open.
