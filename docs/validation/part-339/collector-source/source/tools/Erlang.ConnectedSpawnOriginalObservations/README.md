# Original connected spawn controller observations

Source proposal only. Six scenarios run in separate source-built OTP-29.1.1 VMs pinned to ad05823719d77c8faee87348ea39513d4e2f99c5. No SDK source or SDK outcome supplies an expected value. Publication and manual workflow dispatch remain separate authorized actions.

The probe manually adopts a simulated peer distribution controller inside one original VM. It captures real original request/reply control packets, argument payloads, child state, aliases/monitor state and sequential trace events. It does not establish TCP interoperability between two physical OTP/SDK nodes. All ETF/packet bytes remain retained; no PID or trace normalization is performed.

The tracer snapshot waits for dynamically correlated core events (request header, arguments and reply as applicable). Harness RPC can add further events, so the complete raw event list is not an exact pristine SDK baseline. Serial values are observations, not fabricated expected outputs.

Successful collection requires exact complete/ok ETF markers and actual normal DOWN receipts for controller/tracer and every inbound successful child. Forced/unobserved cleanup is retained as failure. The reused owned-process helper closes streams, reaps the parent and kills any remaining private process-group descendants even after foreground exit. Each compiler/VM operation has a finite timeout and retained command, stdout/stderr and process receipt.

External owned_process.py and passive_etf.py are imported only after their exact Git blob bytes and SHA256 match the manifest. The workflow, local source closure and pinned original source/install/loaded-module identities are separately hash-bound. Changing a dependency requires a fresh reviewed manifest.

The additive alias cases capture native 33/34 input, alias state before and after selection/demonitor/unalias, and real public send/2/send/3 output. Incoming cases require the independent local child monitor to observe normal completion. Selected values and packet contents remain observations requiring later semantic audit; collector completion is not a compatibility result.
