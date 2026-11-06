# application backends

The complete OTP-29.1.1 `application.erl` library has an opt-in C# alternative.
All **39 non-module_info exports** are implemented, including generated
`behaviour_info/1`. Original Erlang source and BEAM remain unchanged. Both
alternatives use the same Erlang MFA interface on the .NET runtime.

## Selection

Choose the backend when constructing the runtime's library selection:

```csharp
RuntimeModuleSelection application = useCSharp
    ? RuntimeModuleSelection.Managed("application", applicationDependencies)
    : Erlang.Otp.OtpKernel.Beam("application", applicationDependencies);
```

Include the choice and its declared dependencies in `RuntimeLibrarySelection`.
The same choice can be expressed with `Managed` or `Beam` in the existing
`erlang-libraries.json` manifest. The original BEAM image must have its matching
hash. `ProcessRuntime.LibrarySelection` records the selected backend, owned
module image, exports, assembly identities and declared dependencies.

The application controller must be running for controller-backed operations.
Load and start the genuine OTP helper services as required by the host boot
profile. Direct library dependencies include `application_controller`,
`application_master`, `dist_ac`, `gen_server`, `maps` and `lists`; their transitive
closure depends on the boot profile. Merely selecting `application` does not
start a kernel or a distributed node. The focused validator boots original
`init`, `code_server` and `application_controller` and uses original Erlang
masters, callbacks, request collections and native process/ETS primitives.

C# owns application loading/start orchestration, serial/concurrent DAG traversal,
optional dependency handling, async request collection and rollback, permission
routing, environment wrappers, group-leader/application/supervisor lookup and
behaviour metadata. It never calls original `application` as an implementation
fallback. Controller/master/distribution support modules retain their original
OTP protocols on .NET, as they do for the Erlang alternative. No external ERTS
is required in production. Selection is at startup; live switching and state
migration are outside this change.

## Validation and scope

[Application validation](validation/application-port-20261106/README.md) records independent acceptance for both
backends against admitted official OTP29.1.1 at
`ad05823719d77c8faee87348ea39513d4e2f99c5`. The primary corpus compares complete
returned terms and exception classes/reasons/stacks. Connected scenarios cover
real application lifecycle, group-leader queries, persistent configuration,
permissions, missing/optional dependencies, serial and concurrent diamond
graphs, failure rollback and forty-app repeated-root traversal. Serial result
order is compared exactly; concurrent success collections are compared by
membership because completion order is unspecified.

Five unchanged `application_SUITE` functions execute with their genuine
`appinc1.app` and `appinc2.app` fixtures: `get_env`, `set_env`,
`set_env_persistent`, `persistent_env` and `set_env_errors`. Oracle admission,
corpus/helper hashes, source blobs and assembly identities are retained.

These are finite Windows checks. The complete upstream suite, distributed
takeover/failover/node-loss, whole-kernel/release boot, resource exhaustion,
all scheduling/platform behaviors and module_info metadata equivalence remain
separate acceptance gates. No universal100% compatibility or performance claim
is made. Full regression and canonical readiness regeneration are deliberately
unrun under the user's focused-check instruction. See the receipt for exact
results, commands and retained initial failures.

The linked validation records the original implementation checkout. The publication check records any replay on the isolated PR base separately.

[Publication replay and hash checks](validation/application-port-20261106/publication/README.md).
