# Interoperability exception record

Authorization: work.md section 2.1 requires C# wherever technically possible and allows unavoidable platform-specific interoperability. Parent coordinator explicitly approved a minimal C ABI veneer under this rule. This artifact does not request a separate policy exception.

Allowed native scope: exact C variadic/va_list ownership and typed extraction, exported Linux enif symbol forwarders and optional FILE* byte-output ABI primitive. C# retains every environment, term, pending exception, allocation policy, formatter algorithm, load decision, ownership join and registry operation. No OTP runtime algorithm is copied into native code.

Reason: ordinary CLR unmanaged exports do not supply a C variadic entry/va_list traversal; CoreCLR JIT function pointers are not ELF named/global exports. A NativeAOT forwarder could address fixed named exports but remains unvalidated and does not eliminate actual C varargs handling. The preferred C veneer must be compiled per verified ABI, not use register-layout guesses.

Provenance: interface definitions are checked against OTP-29.1.1, commit ad05823719d77c8faee87348ea39513d4e2f99c5, headers bound in source-bindings.json. Those pinned headers identify Apache-2.0 and Ericsson AB copyright. Reuse the pinned headers/build inputs rather than silently copying stripped header implementations. Before distributing compiled/native artifacts, the parent must ensure existing consolidated license/notice records cover shipped upstream material and generated declarations. No new license banner, LICENSE file, native binary or redistribution is created here.

Status: design-only record; no C runtime algorithm, build or native execution in this artifact. Any later exception must be recorded separately rather than broadening this scope implicitly.
