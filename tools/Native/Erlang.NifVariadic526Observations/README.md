# Genuine variadic NIF family 526

Source-only proposal. No original or managed execution is claimed by this package. Pin: `ad05823719d77c8faee87348ea39513d4e2f99c5` (OTP 29.1.1). Existing admitted provider, native compiler recipe, bounded owned process launcher and dirty child host are reused.

The C fixture calls real `enif_make_tuple` and `enif_make_list` variadic functions at arities 0, 1, 2, 9 and 17. It does not replace them with array APIs. All run in normal, dirty CPU and dirty IO functions. Nested and repeated children include a 300-element payload, binary, maps and a pending map whose binary key changes after publication into a variadic tuple/list. Full returned ETF retains every constructed value. Internal sharing is exercised by repeated handles, but ETF equality alone does not prove physical alias identity.

Promotion:

1. Copy these seven tool files into the matching repository target and apply `host-subject.patch` after checking its two exact preimages. Optional subject fields default to the existing dirty fixture; declared original module images remain SHA checked. No loader or callback-table substitution occurs.
2. Run `py -3.12 -B tools/Native/Erlang.NifVariadic526Observations/seal-source-plan.py` AFTER all parent runtime/helper/host changes and before acquisition. This writes the source plan and prints its raw SHA. Rebuild the actual host once; this package does not build managed projects.
3. Run `collect.py --official-admission <admission.json> --admission-sha256 <rawSHA> --plan-sha256 <printedSHA> --output <fresh repo artifacts/nif-variadic526/name> --execute-original --with-managed`. Omit execute for prepare-only; omit with-managed for original-only.

One admitted native build and one original VM acquisition precede four independent managed children: ordinary/owned profiles times ordinary/reviewed loader routes. Failed preparation prevents dependent VM runs. Independent child failures accumulate, including missing or invalid completion receipts. Every successful child must prove exact raw AND deterministic full ETF equality, requested profile and route, the same admission SHA, native owner retirement and environment/binary baseline restoration. Full raw original outcomes and launch/compile/owner/provider/cleanup receipts remain immutable; no projection, normalization or expected managed seed is used. The original module cleanup ETF is retained, while the existing managed child separately proves physical image retirement rather than claiming literal cleanup-ETF equality.

Windows x64 first. The source-plan header bytes come from the already-admitted map-family provider recipe; source plans and runtime owner sources are sealed anew after promotion. Existing Windows varargs helper must be built/installed separately before managed build. Its registration and table override must be Windows-only; Linux keeps its existing export-registration/managed callback behavior. For NuGet RID assets the parent binder must use assembly-aware native resolution when the flat Core directory lacks the helper.

Unsupported claims: invalid vararg count/fatal allocation exhaustion, all NIF ABI functions, crypto success, Linux variadic bridge proof, resource-policy equivalence, and closure of any of the 24 known differences. The finite observer deliberately does not manufacture such coverage.
