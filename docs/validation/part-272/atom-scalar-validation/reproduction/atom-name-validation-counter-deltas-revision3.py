from pathlib import Path
import json,hashlib,subprocess
r=Path.cwd();p=r/'artifacts/atom-name-validation-prototype';e=r/'artifacts/atom-name-validation-evidence'
f=p/'tests/Erlang.Tests/AtomNameValidationTestNames.cs';s=f.read_text();s=s.replace('    internal const string Utf8 = "utf8";','    internal const string Utf8 = "utf8";\n    internal const string FreshNamePrefix = "atom_name_registry_delta_272_";\n    internal const string GuidFormat = "N";');f.write_text(s)
f=p/'tests/Erlang.Tests/AtomNameValidationChecks.cs';s=f.read_text();needle='''            Check(reportedCount.Equals(Term.I(AtomNameRegistry.Count)));
            var list''';assert s.count(needle)==1;s=s.replace(needle,'''            Check(reportedCount.Equals(Term.I(AtomNameRegistry.Count)));
            string freshName = AtomNameValidationTestNames.FreshNamePrefix + Guid.NewGuid().ToString(AtomNameValidationTestNames.GuidFormat);
            Check(!AtomNameRegistry.Exists(freshName));
            Term firstCount = await runtime.Modules.Call(context, ErlangModuleNames.Module, SequenceTraceNames.SystemInfo, Term.A(AtomRegistryInfoNames.AtomCount));
            Term freshAtom = await runtime.Modules.Call(context, ErlangModuleNames.Module, AtomNameValidationTestNames.ListToAtom, Term.String(freshName));
            Term addedCount = await runtime.Modules.Call(context, ErlangModuleNames.Module, SequenceTraceNames.SystemInfo, Term.A(AtomRegistryInfoNames.AtomCount));
            Check(((Integer)addedCount).Value == ((Integer)firstCount).Value + 1);
            Term sameAtom = await runtime.Modules.Call(context, ErlangModuleNames.Module, AtomNameValidationTestNames.BinaryToAtom, new BitString(Encoding.UTF8.GetBytes(freshName)));
            Check(sameAtom.Equals(freshAtom));
            Check((await runtime.Modules.Call(context, ErlangModuleNames.Module, SequenceTraceNames.SystemInfo, Term.A(AtomRegistryInfoNames.AtomCount))).Equals(addedCount));
            var list''');f.write_text(s)
f=p/'tools/Erlang.Differential/AtomScalarValidationCases.cs';s=f.read_text();body='L="atom_scalar_delta_native_272_"++integer_to_list(erlang:unique_integer([positive])),C=erlang:system_info(atom_count),A=list_to_atom(L),D=erlang:system_info(atom_count),true=(D=:=C+1),A=list_to_atom(L),D=erlang:system_info(atom_count),A=binary_to_atom(list_to_binary(L)),D=erlang:system_info(atom_count),done.';source='-module(atom_scalar_validation_contract). -export([run/0]). run()->'+body
addition='''        new(
            "atom-scalar-validation-real-count-insertion-and-existing-deltas",
            '''+json.dumps(source)+''',
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(Completion))
        ),
''';assert s.count('    ];')==1;s=s.replace('    ];',addition+'    ];');f.write_text(s)
raw=subprocess.check_output(['git','-C','../otp','show','OTP-29.1.1:erts/emulator/beam/erl_bif_info.c']);(e/'pinned-original/erl_bif_info.c').write_bytes(raw)
old=json.loads((r/'artifacts/atom-name-validation-source-freeze-revision2.json').read_text());rows=[]
for row in old['Files']:
 name=row['Path'];raw=(p/name).read_bytes();original=r/name;rows.append({'Path':name,'BeforeSha256':hashlib.sha256(original.read_bytes()).hexdigest().upper() if original.exists() else None,'Sha256':hashlib.sha256(raw).hexdigest().upper(),'BeforeSource':original.read_text() if original.exists() else None,'SourceContent':raw.decode()})
freeze={'Stage':'Revision3AdditionalCounterDeltaControlsPreBuild','Files':rows,'InitialEightCaseSourceExpectedUnchanged':True,'NewCompiledCases':9,'PreviousActualGeneratedPass':8,'NativeExecuted':False,'CoreRegistrationsAdded':0,'ProductionFiles':5,'PreviousFreezeSha256':hashlib.sha256((r/'artifacts/atom-name-validation-source-freeze-revision2.json').read_bytes()).hexdigest().upper()};f=r/'artifacts/atom-name-validation-source-freeze-revision3.json';f.write_text(json.dumps(freeze,indent=2));print('Revision3 SHA',hashlib.sha256(f.read_bytes()).hexdigest().upper())
