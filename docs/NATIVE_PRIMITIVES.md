# Explicit native primitives

Original OTP libraries execute through `ErlangLibrary` and the selected Erlang/BEAM backend. Native storage primitives are an explicit runtime service, selected separately from pure library backends:

```csharp
RuntimeLibrarySelection libraries = RuntimeLibraryManifest.Load(manifestPath);
await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(
    libraries,
    RuntimeNativePrimitives.EtsStorageCore | RuntimeNativePrimitives.PersistentTermCore,
    CancellationToken.None
);
```

The existing two-argument factory keeps its previous behavior. The default runtime does not silently enable either provider. `runtime.NativePrimitives` reports actual current bindings and their assembly identity. Selecting a pure dependency still requires declaring it in the manifest; selecting native primitives does not authorize undeclared pure imports.

The Windows x64 regex engine is a separate admitted, opt-in owner. Register it
outside an existing registry mutation/startup callback:

```csharp
await RuntimeNativePrimitiveProviders.RegisterRegexAsync411(
    runtime, runtime.Modules, officialOtpAdmissionPath, cancellation);
```

It supplies actual `re:compile/1,2`, `run/2,3` and `version/0` using the pinned
exported PCRE engine. Runtime shutdown owns and drains the library, compiled
patterns and active matching operations; guest magic references do not control
shutdown. A binary/default-option and compile-unicode cohort matches96 complete
original observations. Broader valid options, Unicode lists and altered raw
compiled metadata remain explicit capability refusals. These five bodies are
not five complete API contracts, and the original full re8 image remains held.
Ordinary flag registration cannot enable this provider without admission.

The Windows redirected `prim_tty` profile is instead bound to an exact prepared
original image through `RuntimeBootstrapTty.Register`. Its20 handlers replace
declared NIF stubs while the original Erlang module remains source-owned. Four
HOST groups pass;the matched126 comparison now passes all values,full stacks
and raw ETF. The first120/126 failure remains retained: native errors must
replace the exact Erlang stub frame with actual arguments. No standard boot or
whole TTY compatibility is inferred. This opt-in profile does not change the
default API registry or silently claim console/termcap/locale support.

| Provider | Available native signatures |
| --- | --- |
| `EtsStorageCore` | `new/2`, `whereis/1`, `insert/2`, `lookup/2`, `member/2`, `delete/1,2`, `select/2`, `lookup_element/3,4`, `match/2` |
| `PersistentTermCore` | `get/0,1,2`, `put/2`, `put_new/2`, `erase/1` |

These include the seven ETS and two persistent-term signatures directly used by the pinned original `code_server`. Part410 additionally verifies actual original code_server startup through original init and erl_prim_loader in embedded and two interactive fixtures (207 complete outcomes). This uses explicit ordinary PrepareBeam configuration;the complete strict dependency/native-service closure and standard boot are still required. See [bootstrap setup and limits](OTP_BOOTSTRAP.md).

Providers are installed before strict startup preparation inside the existing registry transaction. Foreign owners, displaced bindings and conflicting pure modules are refused. Failed installation rolls back. Repeated installation accepts only the same authenticated current provider. Supported function values use detached authenticated recipes; ETS process completion waits for owned retirement, while persistent terms survive the creating process and retire at runtime shutdown. Externally held HOST read leases intentionally retain their payload until released.

This is a subset, not full-module compatibility. `persistent_term:info/0` is unavailable pending truthful native-layout accounting. Broader ETS operations, options, heirs and physical scheduling/memory contracts remain open. Original whole native-stub images retain strict admission checks. The full retained persistent-term diagnostic still has 33 differences out of 138 observations; the passing state-independent companion does not replace it.

## Local verification checkpoint

Against admitted original Windows OTP-29.1.1 (`ad05823719d77c8faee87348ea39513d4e2f99c5`): 66 function-storage, 48 ETS-key, 84 ETS-select and 66 configured-manifest observations match complete typed terms, full stacks and raw ETF. The last set executes unchanged original-compiled workers with explicit Managed `erlang` and both native providers.

Final-source verification: 42 focused groups, 350 affected regression groups and 13 existing startup/manifest groups pass. Source layout requires no changes; 744 source guards retain the default 708 API entries. These selected runs do not supersede Part403's accepted full regression or promote default full contracts. No fresh Linux/GitHub execution or publication is claimed. See [changelog volume 0070](changelog/0070.md).


Part410 successor:80/80 focused groups and350/350 affected regressions pass on the same final runtime. Original bootstrap207 and filename198 whole observations match in addition to the prior264. There are776 source guards;default708 API entries and118 full contracts remain unchanged. The13 prior startup checks are included in the80,not an additional80+13 run. The accepted full checkpoint remains Part403.

Part411 in progress:the explicit provider now exposes ETS11+persistent6 signatures. New concurrency234 and lookup_element252 whole raw/typed/source-stack observations pass. match/2 delegates the existing select implementation using the exact pinned match-spec construction;three HOST groups and all186 complete original comparisons pass. The initial signed-zero prediction failure and separate passive gate acceptance remain retained. Mailbox message_queue_data126 and Unicode skip198 comparisons pass. Physical storage/heap/tuning and narrowed tuple-header projection remain explicit limits. See [standard boot progress](STANDARD_BOOT_PROGRESS.md);no whole-module promotion or publication.

Part411 native-provider metadata adds module_info/0,1 on each configured storage
provider:four non-native helpers,17 native bindings unchanged. Metadata describes
the actual implemented registry and managed assembly rather than an original
OTP module digest/export list. Three HOST groups and36 full original metadata
outcomes pass,including the retained wrapper frame on BIF errors. Static strict
import rules remain unchanged;these helpers do not admit the whole original
storage images. Final affected regression1755/1756 retains one exact known
Part404 Unicode stack failure;all24 independent blocks execute. Source guards791.

## Optional managed scheduler boot dependency (Part412)

`RuntimeBootstrapScheduler.Register(runtime, registry)` explicitly installs the
real `erts_internal:scheduler_wall_time/1` bootstrap binding. Exact `true` enables;
other terms disable and return the previous boolean,as in the pinned native
source. Per-runtime state fences actual managed dispatcher lanes and measures
selected-task intervals with Stopwatch. `CaptureManagedSchedulerWallTime()`
returns a detached immutable host snapshot. Repeated true preserves the origin;
false-to-true resets it. Physical ERTS dirty scheduler/CPU statistics are not
implemented by substituting these managed measurements.

This one optional native binding is separate from default708 and from the
whole original erts_internal image. Genuine stock boot14 reaches init started
with this facet plus real Re5,source-owned TTY20,storage and filesystem services.
Its exit0/settled cleanup is boot evidence;the original79 lifecycle comparison
and final affected regression remain separate acceptance gates.

## Explicit source-derived ETS bootstrap profile (Part412)

After the existing EtsStorageCore registration,
`RuntimeBootstrapEtsDeletion.Register(runtime, registry)` installs two genuine
native bodies: `internal_select_delete/2` and `internal_delete_all/2`. Default
ETS11 and provider17 profiles remain unchanged. The standard-boot validation
explicitly compiles15 exact pinned Erlang forms into a separately identified
partial ets image:13 original native stubs capture real bodies,two pure
wrappers remain Erlang,and compiler metadata gives17 actual exports. The
unchanged official ETS51 image still fails missing-body admission. Source
publication replaces the facade and truthfully reports the derived image;
its source-private captures are not reclassified as the old provider ledger.
The derived asset and reproducible source are retained in the validation
fixtures;this is not automatic deployment of whole ETS through strict
ErlangLibrary selection.

Final build16 matches all72 original deletion observations and all79 standard
boot/application lifecycle observations. Final TTY replay is126/126. The
standard profile still uses admitted Windows native/filesystem capabilities;
these results are not Linux/interactive-console/full ERTS compatibility.
Select-delete evaluates detached values outside runtime lock and revalidates
exact table/row identity before commit. Real retirement is joined. Final
reservation/rebuild is O(rows),without a new physical/performance guarantee.
See the [Part412 checkpoint](validation/part-412/native-standard-boot/README.md).
