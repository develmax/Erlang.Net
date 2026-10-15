# Native provider context guard

Source-only narrow revision atop immutable `nif-native-lifecycle-fence300-overlay`. The three-path parent overlay is unchanged. Apply the Gate/Names revisions after it, and the independent one-line PlatformLoader revision against the exact Main parent hash in the manifest. No Main, Git mutation, build, HOST or native execution by the author.

A static managed lifecycle semaphore is shared by all owners/runtimes only within one loaded Runtime assembly copy. The bounded native provider now requires that Runtime assembly to be loaded in AssemblyLoadContext.Default. This makes its process-wide backend exclusion scope explicit instead of silently allowing separate per-ALC gates for the same physical native library. The default context loads one Runtime assembly identity; arbitrary non-default native providers are refused.

The guard runs before lifecycle semaphore acquisition in Enter, and from NifNativePlatformLoader.VerifyPlatform before all native library Open paths, including the export bridge and ABI probes. It does not alter managed language/module execution, generated-module loading, or general custom AssemblyLoadContext behavior. No general cross-ALC native support is claimed.

An unsupported provider fails with the domain-specific NotSupportedException before native entry. Current trusted SDK execution uses the default provider and retains the existing cancellation, cleanup and lock-order behavior. The author has not executed that compatibility check; the root must validate the merged native SDK contracts and can add a separate unsupported-provider control without executing native callbacks.
