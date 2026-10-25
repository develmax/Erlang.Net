# Part492 — atom conversions, wrapper stacks and name binaries

Oracle: OTP-29.1.1, `ad05823719d77c8faee87348ea39513d4e2f99c5`. Nine default atom-conversion APIs are audited. All569 values and2845 complete stack outcomes pass in13 groups (3414 whole comparisons). Six read/conversion contracts are promoted; three fresh-name creators remain incompatible at table saturation. API presence stays708.

## Actual repairs

The first runtime comparison passes9/12 groups. All560 ordinary values match; three one-argument wrappers have wrong stack argument/location metadata. Pinned erlang.erl implements atom_to_binary/1, binary_to_atom/1 and binary_to_existing_atom/1 with try/catch followed by error_with_info. Reproduce its original argument list, formatter map, source file/line and caller/depth behavior. PortErrorSourceLocations.ErrorWithInfo already denotes that exact same helper, so reuse it rather than adding a coincidentally equal line constant. The immutable error_info map is shared like the source literal. Full pre-fix traces remain in before-stacks.

Pinned erl_unicode.c returns the atom's stored ap->u.bin for UTF8/unicode and ASCII Latin1. The previous code allocated a new BitString on every conversion. AtomUtf8BinaryCache now publishes one immutable binary per exact registered name. GetOrAdd returns the published entry under contention; the cache key set is a subset of the permanent bounded atom-name table. Equal atom words share the name binary even when represented by different managed Atom objects. There is no Unicode normalization or case folding. Non-ASCII Latin1 still validates the255 bound and allocates a fresh transcoding. This avoids repeated wrapper/binary allocation; no measured speedup is claimed.

The additional actual native same observations verify repeated empty/nonempty UTF8 results, empty/nonempty list result identity and shared error_info maps across all three wrappers. Atom-to-list enumerates Unicode scalars into fresh list cells; empty names return nil. Existing-name lookups validate complete type/encoding/range/proper-list inputs and return an existing word or badarg without adding a name. source-audit.json binds every complete partition, source and resource. Physical allocation limits/timing and whole compiler completion are separate.

## Own-state and complete observations

Initial existing-name probes did not explicitly establish equivalent table state across independent VMs. Keep that initial acquisition under artifacts/contracts492/original as superseded preparation. Actual compared sources explicitly create valid present names in each VM before existing-name lookup. Guaranteed-missing names remain uncreated; invalid inputs invoke the target directly. ownstate-correction.json records the adjustment. Do not equate unrelated table populations or project away mismatches.

The13 final subjects retain whole values and default/depth0/1/2/8 stacks, including Unicode scalars, supplementary/noncharacters, Latin1, empty/NUL names,255/256 bounds, malformed UTF8, improper lists, invalid encodings and foreign PID/port/reference/function arguments. Function atoms use the existing opaque barrier; disassembly proves dynamic apply. All6427 provider files match before/after and owned children settle. Existing monitored worker/support and actual original same-facet ownership are reused. No field normalization occurs.

All65 preceding/new BIF contract groups pass on the final rebuilt assembly:31782 whole comparisons. Local8961 readiness is explicitly composite: independently verified continuous parent8948 plus13 new groups, with every preceding52 contract group rerun. Exact current-head continuous CI/full cold consumers remain required.

## Three real capacity limitations

The pinned index implementation terminates its VM when a valid new atom cannot be inserted into a full table. A fresh owned original VM with capacity16384 reports its actual initial count, exits1 with the exact atom_tab capacity diagnostic and never enters the surrounding Erlang catch. The process settles and the provider inventory is unchanged. The generic owned helper correctly rejects its nonzero exit; the dedicated observer accepts only that strictly checked expected termination. Unknown exits, timeouts, launch and cleanup failures remain failures. No crash dump artifact is collected.

Managed AtomNameRegistry.Observe instead throws catchable ErlangException(system_limit) at its own maximum. That branch is source-inspected; no live full managed table or paired absolute atom populations are claimed. This affects list_to_atom/1 and binary_to_atom/1,2. Ordinary conversions and complete traces pass, but these three entries become Known differences, not Verified. Atom-name length system_limit is a separate correctly catchable condition and is not confused with table saturation. Fixing VM termination and table ownership remains open.

## Progress and delivery

Registered708, delta0. Full153->159 (+6), pending530->521, known25->28. The three newly identified capacity gaps are not completed contracts. Original559 cohort41 closed/518 remaining (7.33%); remaining contains515 pending and3 known. Six reclassified audits are additional. erlang62 verified/273 pending/16 known of351; other module counts stay unchanged. All1687 prior source guards are unchanged; add five current atom dependency guards,1692 total. Three production files change, including the new cache. Matching native plan rows and the new cache source/seals advance without claiming native ABI acceptance.

Parent491 exact headb7e5e2144 is independently verified green:8948/8948,28368 whole comparisons, all17 cold consumers,1321-image warm/cold catalogs and actual cleanup. parent-ci-proof.json retains the receipt. That parent does not prove this changed runtime. The broad31-difference/559-contract request is unfinished. LICENSE.local.txt remains excluded.
