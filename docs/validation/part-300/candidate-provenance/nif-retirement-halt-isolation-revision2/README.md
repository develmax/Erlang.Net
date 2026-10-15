# Retirement-originated immediate halt isolation

Source-only; no execution/Main/build/Git/native changes. Apply host/ independently first for a bounded failing regression against the buggy runtime, then production/ for the correction. The new public HOST method RetirementRequestsImmediateHalt is individually invocable; Run also includes it. HOST helpers reuse existing install/admission checks, with Register generalized only to accept a module name (existing default unchanged).

The first cleanup checks direct self-Dispose refusal, requests/awaits public immediate halt, and stays alive until the second independently owned cleanup is actually reached. The second owner blocks quiescence behind its separate release barrier; both must clean once and halt quiescence succeed. Previous code inherits the retirement AsyncLocal, refuses CloseNifLifetimeAsync and cannot reach the second owner while the first remains active. Every explicit wait uses the existing five-second deadline; test-finally releases both barriers. Root controls <=30 second external invocation timeout.

Production change clears both initialization and retirement ownership for the independent FinishHalt actor, as RuntimeActorDispatcher already does. Direct DisposeAsync self-join guard remains unchanged.

Other reviewed scope: no registry monitor synchronously waits native cleanup; init/unload take global native lifecycle semaphore before per-owner monitor; actual trusted primitive callbacks do not enter lifecycle ownership. Future arbitrary callbacks must preserve that contract. Default-ALC refusal avoids independent duplicate provider gates. Coarse native serialization is a bounded replacement for pinned OTP erl_nif.c2559-2617 unload/load exclusion, not its resource/dirty-work finisher implementation or exact whole-VM shutdown contract. Generic NIF ABI remains out of scope.

Revision1 preparation aborted on a CRLF-specific match against formatted LF source. Its incomplete overlay remains marked FAILED-PREPARATION and must not be applied. This revision binds actual parent bytes and writes LF.
