# Native child ports, original TCP and kernel listener — Part455

Baseline: **OTP-29.1.1**, commit **ad05823719d77c8faee87348ea39513d4e2f99c5**. Product runtime **5B34A98A785E0161D07DEB5E3042584014E04C054987DF51061DBC87C6FD3F07**; private bridge **540677E29D6FF508CECFEEEF0742F5EC0466AC99B8164F3C767946B7232B295B**.

## Accepted scope

- Actual private child creation/adoption, supplied native data, construction hold, creator unwind and physical stop. Three native feature controls pass. They are not OTP differential tests: the public Windows driver table does not expose this private capability.
- Unchanged original `inet_drv.c` performs real TCP loopback. The complete original observer outcome matches under four explicit injective actor/port roles: ownership/link, packet4 binary/passive traffic in both directions, accepted socket survival after listener close, joined client and closed ports. Actual runtime/environment leases settle.
- Original standard `init`/kernel/application boot uses the real TCP driver and separately owned local EPMD. Its named listener registers successfully and processes an incoming distribution name packet, replying `sok`. The complete observation matches the original VM under five explicit roles: observer/kernel/EPMD PIDs, client port and ephemeral listener endpoint. All other node/state/name-packet/status/error fields are retained.
- Every export in `net_kernel`47, `erl_epmd`22, `inet_tcp_dist`32, `dist_util`16, `gen_tcp`16, `inet`101, `inet_tcp`25 and `prim_inet`62 belongs to its original prepared BEAM image. These export counts authenticate source ownership; they are not full compatible API counts.
- Three new permanent groups cover independent driver scheduling, genuine local ETF identities and own-resource occurrence rebinding. The last preserves foreign equal-spelling values and map keys, rejects malformed bindings and traverses a100000-element list iteratively.
- Actual DDLL native8/retained36 regression runs pass with both the new private bridge and the preserved ordinary old bridge. Ordinary loading therefore remains usable without private exports.

`acceptance.json` binds the accepted observations and final binaries. `files.json` hashes each retained payload and `raw-evidence.zip`, whose single entry is `raw-evidence.tar.xz`. Original paths remain inside the TAR. Package/native binaries are not duplicated; source bindings, manifests, compile logs and hashes identify them. Existing shared harness sources are retained for replay. Absolute local paths in historical recipes identify the execution workspace and need rebasing elsewhere.

Comparators decode both complete outcomes, map only declared roles injectively, compare complete typed terms and re-encode both complete mapped terms to compare ETF. This is not a claim of unchanged raw original byte order or a stack projection. Original ETF files remain available. Listener observation proves initial incoming status, **not** authentication, a connected two-node channel, reconnect or full network module compatibility.

## Retained failures and limits

Keep the initial2/3 child fixture failure and actual scheduler trace; early TCP send timeouts from missing LOCAL_EXT decode; successful traffic followed by container cleanup deadlock; failed build attempts; the invalid-binding test expectation; listener prerequisite failures and the genuine pre-name listener identity timeout. They remain failed historical evidence, not accepted outcomes. Final repeated TCP and child proofs use the runtime hash above.

The dynamic container wrapper performs linked-driver retirement; it does not establish complete builtin-driver visibility parity. The public ABI layout remains unchanged. Generic external function transport, local-reference epoch transitions, wider construction/rollback and `port_connect` death/busy/ACK schedules, UDP, complete TCP options/errors and cross-platform private capability remain open. No broad performance or module100% claim is made.

Default registry APIs **708**, verified full contracts **118**, delta **0/0**; catalog **8837** (+3 groups), source guards **1647** (+20). Source guards verify every admitted current source hash. Main42e83c9c already passed exact CI40013502571 with8834/8834 and both packages; feature-head full CI and package results are recorded separately in the PR after publication. Native loopback/listener evidence here is local Windows execution, distinct from the full GitHub CI suite.

Upstream reference copyright is preserved. No generated license headers are added; `LICENSE.local.txt` is excluded.
