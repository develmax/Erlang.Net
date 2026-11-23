# Closed atom dispatch with alias returns and finite guards

The comparison starts from the full uncached Part 1837 release artifacts and the frozen source overlay with SHA256 `9BE82DBC068947060880683C3AD65196078F67DFB46223D7465275B90F5D68C6`. It uses Windows OTP 29.1.1 at `ad05823719d77c8faee87348ea39513d4e2f99c5`. The separate optional emit-decode experiment remains in its own artifact with a backup of its source edits.

The Part 1823 ranking records 351 modules and 1,574 functions in the historical `select_val` versus `is_eq_exact` group. The ten inspected release examples have several source causes. The patch addresses the closed unary atom dispatch subset represented by explicit alias returns, mixed alias and constant returns, and a finite atom equality guard. It reuses the existing result and tail-call dispatch and finite guard reader. It rejects duplicate keys before emitting any bytes. The OTP `beam_core_to_ssa` selector combines equal code and sorts keys. Aliases that return the incoming value share one body that returns X0. The existing failure label supplies `function_clause`. The patch introduces no dependency or runtime MFA changes.

The formatted eight-module candidate retains 2 Equal, 6 Different, 0 Unsupported and 0 SourceRejected. No release image gained or lost full-file equality. Only three modules changed raw bytes. Their only changed normalized functions, `ct_telnet:type/1`, `diameter_service:type/1` and `eldap:v_deref/1`, now match OTP. The two-worker comparison took 10.721 seconds on the final build. ERTS prepared all three changed images.

The deterministic fixture covers three forms of closed selector and matches the full pinned BEAM with SHA256 `5C000BBEAA4192538C3564BCF63CD9A9CC48CF06D014C0250A49AD11B9DDCFDB`. The before compiler produced a different fixture file. The pinned, before, and candidate images agree on all 12 ERTS outcomes, including the original argument and source location on rejected input. All three selected compiler checks passed from the 9,766-case registry. Compiler and test builds reported zero warnings and errors.

Remaining cases in the historical group include tag-by-arity selection, chained list patterns, nested selectors and a typed fallback after an alias. The broad historical group count should not be used as this patch's reach. The first diagnostic attempts had Windows line-delimiter errors and are retained with their original logs. The corrected byte-oriented comparison succeeded.

- Source and permanent fixture patch in `closed-atom-identity.patch`.
- Cohort and immutable paired paths in `after-final.json`, `status-delta.json`, `changed-paths.tsv` and `after-final.json.timings.json`.
- Function comparison and release loading in `compare-functions-final.log`.
- Fixture execution and test receipts in `fixture-execution.log`, `focused.json`, `compiler-build-final.log` and `test-build.log`.
