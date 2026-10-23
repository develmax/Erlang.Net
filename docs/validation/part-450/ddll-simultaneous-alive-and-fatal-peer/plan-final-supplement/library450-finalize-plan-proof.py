from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd(); sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
queue=r/'docs/remaining-work/architecture-and-compatibility-queue.md'
extra='''
## A2/A3 prerequisite — source, assembly and test-boundary crosswalk

The original work.md section4 proposes26 src and10 test directories. Its exact qualifications are "Suggested structure" and "Adjust project boundaries when needed"; acceptance preserves justified library/test/dependency boundaries, not a mechanical requirement for36 projects. Current Erlang.Net.slnx has7 src projects and one executable test project, tests/Erlang.Tests. Additional tools, examples and benchmarks are separate and must not be miscounted as those test assemblies. Current Compiler references Otp.Behaviours and Runtime; the executable tests reference Compiler, Serialization, Otp.Behaviours and multiple tool/probe assemblies. These are current dependencies to assess, not proof of a cycle or missing functionality.

Before extraction or layout implementation, record a complete matrix: **expected section4 item → actual source path/assembly/test project → matched/merged/renamed/missing boundary → gap or explicit justification → proposed dependency and validation ownership**. Cover every suggested source and test item, including Core/Terms/PatternMatching, Runtime/Processes/Scheduling/Messaging/Distribution, Serialization/Bifs/Modules, Kernel/Stdlib/Sasl/Applications/Behaviours, compiler frontend/semantics/code generation, C# preprocessor/MSBuild/analyzers/tooling, Interop/Diagnostics, and all ten test categories. A missing named assembly is not evidence that its functionality is absent. Reuse actual implementation and test inventory; identify what is intentionally combined and what needs separation.

This crosswalk is mandatory planning input to A2/A3: preserve and justify library, test and dependency boundaries, CI selection, resources, generators and documentation coverage. Merely moving root-level files into folders does not satisfy it. Update the intended dependency graph and target layout against this matrix before small migration batches. The matrix and migration are not completed by this450 documentation supplement; no projects are moved or new functional tests run here.

## A4 — mandatory future scheduler architecture

Source read in full: D:/Distr/Work/Codex/Erlang.Net/mandatory scheduler architecture.md,1204 bytes, SHA256 A19FB1A4E5FF8EC994FA09F735BE64B30AA013ED61BD3FAADF700C3E2B4D463B. This future requirement follows the established P0→P1 priorities; it does not interrupt450 or start scheduler implementation now.

Implement one execution engine with three interchangeable strategies: CompatibleScheduler prioritizes Erlang/BEAM semantics, fairness, preemption, process priorities and correct signals; OptimizedScheduler uses CLR work stealing, JIT-friendly execution, efficient layouts, useful CPU affinity and fewer allocations without violating semantics; DeterministicScheduler controls ordering and reproducible scheduling decisions for deterministic concurrency tests. All share one process model, engine, mailboxes, signals and OTP implementation. Select at runtime without recompiling Erlang; compiler and C# preprocessor output must remain scheduler-independent. Do not duplicate runtimes or OTP, or remove existing alternatives.

Gap: existing managed scheduling controls do not establish these three strategies or their runtime selector. Plan the selector/dependency boundary and inventory existing scheduling behavior before implementation. Acceptance requires the same semantic suite for all three and independent per-mode benchmark results. Derive common workloads and conditions from work.md section17: spawning, throughput/latency, selective receive, supervision, timers, binaries, serialization and distribution; measure applicable allocation/GC/CPU/memory/latency/scaling under identical inputs, toolchain, environment and correctness gates. The scheduler specification sets no numerical threshold or concrete workload parameters; define and record a fair protocol before runs rather than inventing targets or speedup claims. Preserve per-mode correctness, performance and limitations separately. This is planned only, without scheduler edits, benchmark execution or compatibility credit.
'''
text=queue.read_text(encoding='utf-8'); assert '## A4 — mandatory future scheduler architecture' not in text
queue.write_text(text+extra,encoding='utf-8')
for path in ['docs/WORK_PLAN.md','docs/remaining-work/INDEX.md']:
 p=r/path; text=p.read_text(encoding='utf-8')
 marker='## Post-450 owner priority' if 'WORK_PLAN' in path else '## Nearest owner queue'
 assert marker in text
 pos=text.index('\n\n',text.index(marker))+2
 note='Mandatory planning supplement: A2/A3 first require the complete work.md §4 suggested26-source/10-test → actual assembly/test/dependency crosswalk, with justified merged/renamed/missing boundaries and CI/resource coverage; folder moves alone do not meet it. Current7 src/one executable test project is an inventory, not missing-functionality proof. A4 separately queues the fully read, hash-bound mandatory scheduler specification: CompatibleScheduler/OptimizedScheduler/DeterministicScheduler share one engine and OTP, runtime selection, scheduler-independent output, common correctness suite and fair separate benchmarks. These future tasks preserve P0→P1 priority and are not implemented by450. See the single architecture/compatibility queue.\n\n'
 p.write_text(text[:pos]+note+text[pos:],encoding='utf-8')
p=r/'docs/changelog/0079.md';p.write_text(p.read_text(encoding='utf-8')+'\n- Documentation-only follow-up: A2/A3 require a complete original section4 source/assembly/test/dependency crosswalk before migration, preserving justified boundaries rather than enforcing suggested project names. Record the actual hash-bound mandatory three-strategy scheduler specification as future A4 after P0→P1; one engine/OTP, runtime selection, common correctness and separate fair benchmarks. No scheduler/project changes or additional execution.\n',encoding='utf-8')
audit=r/'artifacts/library450-audit-plan.py';text=audit.read_text();text=text.replace("receipt={'Passed'", "assert 'complete matrix:' in queue and '## A4 — mandatory future scheduler architecture' in queue\nassert 'A19FB1A4E5FF8EC994FA09F735BE64B30AA013ED61BD3FAADF700C3E2B4D463B' in queue\nreceipt={'Passed'")
audit.write_text(text)
subprocess.run([__import__('sys').executable,'-B',str(audit)],check=True)
out=r/'docs/validation/part-450/ddll-simultaneous-alive-and-fatal-peer';archive=json.loads((out/'archive-map.json').read_bytes())
supplement=[audit,r/'artifacts/library450-plan-supplement-final.json',Path(__file__).resolve(),queue,r/'docs/WORK_PLAN.md',r/'docs/remaining-work/INDEX.md']
for src in supplement:
 dst=out/'plan-final-supplement'/src.name;assert not dst.exists();dst.parent.mkdir(exist_ok=True);dst.write_bytes(src.read_bytes())
 archive.append({'Path':dst.relative_to(out).as_posix(),'Gzip':False,'RawBytes':dst.stat().st_size,'RawSha256':sha(dst)})
(out/'archive-map.json').write_text(json.dumps(archive,indent=2)+'\n')
manifest=[{'Path':p.relative_to(out).as_posix(),'Bytes':p.stat().st_size,'Sha256':sha(p)} for p in sorted(out.rglob('*')) if p.is_file() and p.name!='manifest.json']
(out/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
stage=(r/'artifacts/library449-stage-audit.py').read_text().replace('library449','library450').replace('part-449/ddll-incoming-and-peer-restart','part-450/ddll-simultaneous-alive-and-fatal-peer')
(r/'artifacts/library450-stage-audit.py').write_text(stage)
print(json.dumps({'Passed':True,'FinalProofFiles':len(manifest)+1,'ArchiveFiles':len(archive),'DocumentationOnly':True}))
