from pathlib import Path
import json,hashlib,shutil,subprocess
r=Path.cwd();a=r/'artifacts';root=a/'packet-pending-correction-prototype';tool=root/'tools/Erlang.DistributionPacketContrasts';tool.mkdir(parents=True,exist_ok=True)
for name in ['Directory.Build.props','global.json','NuGet.Config']:
 if (r/name).exists():shutil.copyfile(r/name,root/name)
for p in (r/'tools/Erlang.DistributionPacketContrasts').iterdir():
 if p.is_file():shutil.copyfile(p,tool/p.name)
source=(tool/'distribution_packet_probe.erl').read_text()
source=source.replace('    Owner = self(),\n', '    Owner = self(),\n    Pending = erts_internal:new_connection(packet_remote@localhost),\n    save("pending-handle.etf", term_to_binary(Pending)),\n',1)
source=source.replace('        Handle = receive {ready, Controller, H} -> H after 5000 -> error(controller_timeout) end,\n        save("inputs.etf", term_to_binary({packet_remote@localhost, flags(), 1})),', '''        Handle = receive
                     {ready, Controller, H} -> H;
                     {'DOWN', Monitor, process, Controller, ReadyReason} ->
                         put(packet_ready_down, {observed, ReadyReason}),
                         save("controller-ready-failure.etf", term_to_binary(ReadyReason)),
                         error({controller_died_before_ready, ReadyReason})
                 after 5000 -> error(controller_timeout) end,
        save("inputs.etf", term_to_binary({packet_remote@localhost, flags(), 1, Pending})),''')
old='''        Controller ! stop,
        receive {'DOWN', Monitor, process, Controller, Reason} ->
            save("cleanup-controller.etf", term_to_binary(Reason))
        after 5000 -> exit(Controller, kill),
                      receive {'DOWN', Monitor, process, Controller, Reason} ->
                          save("cleanup-controller.etf", term_to_binary({forced, Reason}))
                      after 5000 -> error(cleanup_timeout) end
        end'''
assert old in source
source=source.replace(old,'        cleanup_controller(Controller, Monitor)')
method='''cleanup_controller(Controller, Monitor) ->
    case erase(packet_ready_down) of
        {observed, ObservedReason} ->
            save("cleanup-controller.etf", term_to_binary(ObservedReason));
        undefined ->
            Controller ! stop,
            receive {'DOWN', Monitor, process, Controller, CleanupReason} ->
                save("cleanup-controller.etf", term_to_binary(CleanupReason))
            after 5000 -> exit(Controller, kill),
                          receive {'DOWN', Monitor, process, Controller, ForcedReason} ->
                              save("cleanup-controller.etf", term_to_binary({forced, ForcedReason}))
                          after 5000 -> error(cleanup_timeout) end
            end
    end.

'''
source=source.replace('controller(Owner) ->',method+'controller(Owner) ->',1)
(tool/'distribution_packet_probe.erl').write_bytes(source.encode());digest=hashlib.sha256(source.encode()).hexdigest().upper()
names=(tool/'PacketNames.cs').read_text();oldhash=json.loads((tool/'source-plan.json').read_text())['SourceSha256'];assert oldhash in names
names=names.replace(oldhash,digest).replace('        "kernel-notice.etf"','        "kernel-notice.etf",\n        "pending-handle.etf"');(tool/'PacketNames.cs').write_bytes(names.encode())
plan=json.loads((tool/'source-plan.json').read_text());plan['RequiredArtifacts'].append('pending-handle.etf');plan['PreviousPublishedSourceSha256']=oldhash;plan['SourceSha256']=digest;plan['NativeExecuted']=False;plan['RevisionNote']='Create pending dist entry before process-controller adoption. Capture and retain pending handle through inputs after adoption; observe monitored ready DOWN immediately and preserve its full reason without a cleanup timeout replacing it. Original271 idle-entry failure remains unchanged.';plan['OptionalFailureArtifacts']=['controller-ready-failure.etf','exception.etf'];plan['InputsSchema']='{RemoteNode,Flags,RemoteCreation,PendingHandle}; first three original fields retained, pending handle added for explicit liveness across adoption';(tool/'source-plan.json').write_bytes((json.dumps(plan,indent=2)+'\n').encode())
readme=(tool/'README.md').read_text().replace('all seventeen required raw artifacts','all eighteen required raw artifacts');readme+='\nThe corrected probe first creates a pending connection before controller adoption and saves its handle. The handle remains live through the post-adoption inputs record. A monitored controller death before readiness is recorded immediately with its actual reason; cleanup preserves that observation. This is a new published source version, not a retry or normalization of the earlier idle-entry failure.\n';(tool/'README.md').write_bytes(readme.encode())
workflow=(r/'.github/workflows/distribution-packet-contrasts.yml').read_text();workflow=workflow.replace('  workflow_dispatch:\n','''  workflow_dispatch:
    inputs:
      capture_mixed:
        description: Capture the independent mixed-map probe as well
        type: boolean
        default: true
''',1);workflow=workflow.replace('        if: ${{ !cancelled() }}','        if: ${{ !cancelled() && inputs.capture_mixed }}',1);workflow=workflow.replace("        if: ${{ !cancelled() && steps.mixed-tool.outcome == 'success' && steps.pinned-build.outcome == 'success' }}", "        if: ${{ !cancelled() && inputs.capture_mixed && steps.mixed-tool.outcome == 'success' && steps.pinned-build.outcome == 'success' }}");workflow=workflow.replace('''      - uses: actions/upload-artifact@v7
        if: always()
        with:
          name: pinned-mixed-map-identity''','''      - uses: actions/upload-artifact@v7
        if: always() && inputs.capture_mixed
        with:
          name: pinned-mixed-map-identity''')
w=root/'.github/workflows/distribution-packet-contrasts.yml';w.parent.mkdir(parents=True,exist_ok=True);w.write_bytes(workflow.encode())
owned=['tools/Erlang.DistributionPacketContrasts/'+n for n in ['distribution_packet_probe.erl','PacketNames.cs','source-plan.json','README.md']]+['.github/workflows/distribution-packet-contrasts.yml']
freeze={'Status':'BeforeAnyChangedTargetExecution','BaseHead':subprocess.check_output(['git','rev-parse','HEAD']).decode().strip(),'Original271FailurePreserved':True,'PriorMixed271NotRerunFor272':True,'SourceSha256':digest,'RequiredArtifacts':18,'NativeExecuted':False,'ProductionCodecChanges':0,'Files':[{'Path':path,'BeforeSha256':hashlib.sha256((r/path).read_bytes()).hexdigest().upper(),'BeforeSource':(r/path).read_text(),'Sha256':hashlib.sha256((root/path).read_bytes()).hexdigest().upper(),'SourceContent':(root/path).read_text()} for path in owned]}
out=a/'packet-pending-correction-source-freeze-revision2.json';out.write_bytes((json.dumps(freeze,indent=2)+'\n').encode());print(digest,hashlib.sha256(out.read_bytes()).hexdigest().upper())
