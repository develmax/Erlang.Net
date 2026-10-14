from pathlib import Path
import json,hashlib,subprocess
r=Path.cwd();p=r/'artifacts/atom-name-validation-prototype';e=r/'artifacts/atom-name-validation-evidence'
f=p/'tools/Erlang.Differential/DiagnosticCaseKind.cs';s=f.read_text();assert s.count('    BasedInteger\n')==1;s=s.replace('    BasedInteger\n','    BasedInteger,\n    IllegalAtomName,\n    IllegalVariableName\n');f.write_text(s)
f=p/'tools/Erlang.Differential/CompiledDiagnosticCases.cs';s=f.read_text();s=s.replace('.. BasedIntegerDiagnosticCases.All','.. BasedIntegerDiagnosticCases.All, .. AtomNameDiagnosticCases.All');f.write_text(s)
f=p/'tools/Erlang.Differential/CompiledDiagnosticExpectations.cs';s=f.read_text()
for method,value in [('Code','AtomNameDiagnosticExpectations.Code'),('Message','AtomNameDiagnosticExpectations.Message(fixture)'),('ReferenceOutcome','AtomNameDiagnosticExpectations.Outcome(fixture)')]:
 start=s.index('public static '+('Term' if method=='ReferenceOutcome' else 'string')+' '+method+'(CompiledDiagnosticCase fixture)');at=s.index('    {\n',start)+len('    {\n');s=s[:at]+'        DiagnosticCaseKind.IllegalAtomName or DiagnosticCaseKind.IllegalVariableName => '+value+',\n'+s[at:]
f.write_text(s)
f=p/'tools/Erlang.Differential/CompiledModuleProtocol.cs';s=f.read_text();needle='DiagnosticCaseKind.CharacterLiteralUnterminated or DiagnosticCaseKind.BasedInteger';assert s.count(needle)==1;s=s.replace(needle,needle+' or DiagnosticCaseKind.IllegalAtomName or DiagnosticCaseKind.IllegalVariableName');f.write_text(s)
f=p/'tools/Erlang.Differential/AtomScalarValidationCases.cs';s=f.read_text();valid=[]
for name,token in [('quoted',"'"+'a'*255+"'"),('bare','a'*255),('variable','A'*255),('quoted-supplementary',"'"+'\U00010000'*255+"'")]:
 body=token+'=done,'+token if name=='variable' else token+',done'
 source='-module(atom_scalar_validation_contract). -export([run/0]). run()->'+body+'.';valid.append('''        new(
            '''+json.dumps('atom-scalar-validation-lexer-valid-'+name)+''',
            '''+json.dumps(source,ensure_ascii=True)+''',
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
''')
s=s.replace('    ];',''.join(valid)+'    ];');f.write_text(s)
source=subprocess.check_output(['git','-C','../otp','show','OTP-29.1.1:lib/stdlib/src/erl_scan.erl']);(e/'pinned-original/erl_scan.erl').write_bytes(source)
f=p/'tools/AtomNameValidationFocus/Program.cs';s=f.read_text();s=s.replace('using Erlang;','using Erlang;\nusing Erlang.Compiler;');needle='Console.WriteLine(JsonSerializer.Serialize';at=s.index(needle);check='''var diagnoses = new List<object>();
foreach (var fixture in AtomNameDiagnosticCases.All)
{
    try
    {
        using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);
        throw new Exception("Lexer accepted overlong identifier");
    }
    catch (CompileException exception)
    {
        bool pass = exception.Code == AtomNameDiagnosticExpectations.Code && exception.Message == AtomNameDiagnosticExpectations.Message(fixture) && exception.Offset == AtomNameDiagnosticExpectations.Offset(fixture);
        if (!pass) failures++;
        diagnoses.Add(new { fixture.Name, Pass = pass, exception.Code, exception.Message, exception.Offset });
    }
}
''';s=s[:at]+check+s[at:];s=s.replace('Generated = rows, Failed = failures','Generated = rows, Diagnoses = diagnoses, Failed = failures');f.write_text(s)
old=json.loads((r/'artifacts/atom-name-validation-source-freeze-revision4.json').read_text());owned=[x['Path'] for x in old['Files']]+['src/Erlang.Compiler/Lexer.cs','src/Erlang.Compiler/LexerDiagnostics.cs','tools/Erlang.Differential/AtomNameDiagnosticCases.cs','tools/Erlang.Differential/AtomNameDiagnosticExpectations.cs','tools/Erlang.Differential/DiagnosticCaseKind.cs','tools/Erlang.Differential/CompiledDiagnosticCases.cs','tools/Erlang.Differential/CompiledDiagnosticExpectations.cs','tools/Erlang.Differential/CompiledModuleProtocol.cs'];rows=[]
for name in owned:
 raw=(p/name).read_bytes();original=r/name;rows.append({'Path':name,'BeforeSha256':hashlib.sha256(original.read_bytes()).hexdigest().upper() if original.exists() else None,'Sha256':hashlib.sha256(raw).hexdigest().upper(),'BeforeSource':original.read_text() if original.exists() else None,'SourceContent':raw.decode()})
freeze={'Stage':'Revision5LexerTypedErrorRoutePreBuild','Files':rows,'PreviousFreezeSha256':hashlib.sha256((r/'artifacts/atom-name-validation-source-freeze-revision4.json').read_bytes()).hexdigest().upper(),'PriorCompiledCasesNineSourceExpectedUnchanged':True,'PriorIdentifierCasesElevenSourceExpectedNamesUnchanged':True,'NewCompiledCases':13,'NewDiagnoses':4,'PreservedLexRegressionReceiptSha256':'A3A69CF389C4DFC73A8C0D7EEDF7903C04EC9171C6A3A133306864EC1D136CDE','NativeExecuted':False,'MainFullExpected':6515,'MainFullExecuted':False};f=r/'artifacts/atom-name-validation-source-freeze-revision5.json';f.write_text(json.dumps(freeze,indent=2));print('Revision5prebuildSHA',hashlib.sha256(f.read_bytes()).hexdigest().upper(),'owned',len(rows))
