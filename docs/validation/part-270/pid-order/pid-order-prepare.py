import pathlib,subprocess,json,hashlib,struct
root=pathlib.Path.cwd(); out=root/'artifacts/pid-order-prototype'; out.mkdir(exist_ok=True)
paths=subprocess.check_output(['git','ls-files','-z']).decode().split('\0')
seed=[]
for p in paths:
 if not p or not (p.startswith(('src/','tools/','tests/','benchmarks/','examples/')) or '/' not in p): continue
 f=root/p
 if not f.is_file(): continue
 target=out/p;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(f.read_bytes());seed.append({'Path':p,'Sha256':hashlib.sha256(f.read_bytes()).hexdigest()})
(root/'artifacts/pid-order-seed.json').write_text(json.dumps({'Head':subprocess.check_output(['git','rev-parse','HEAD']).decode().strip(),'Files':seed},indent=2))
def pid(node,num,serial,creation):
 n=node.encode();return bytes([131,88,119,len(n)])+n+struct.pack('>III',num,serial,creation)
def port(node,num,creation):
 n=node.encode();return bytes([131,120,119,len(n)])+n+struct.pack('>QI',num,creation)
def binary(data):return 'binary_to_term(<<'+','.join(map(str,data))+'>>)'
pairs=[('pid-serial-before-node',pid('z@host',0,0,9),pid('a@host',0,1,0)),('pid-number-before-node',pid('z@host',1,0,9),pid('a@host',2,0,0)),('pid-serial-before-number',pid('z@host',4294967295,0,0),pid('a@host',0,1,0)),('pid-number-before-creation',pid('a@host',1,2,9),pid('a@host',2,2,0)),('pid-node-after-payload',pid('a@host',3,4,9),pid('z@host',3,4,0)),('pid-creation-after-payload',pid('a@host',3,4,0),pid('a@host',3,4,4294967295)),('pid-unicode-node',pid('\ue000@host',3,4,0),pid('\U00010000@host',3,4,0)),('port-unicode-node',port('\ue000@host',9,9),port('\U00010000@host',0,0)),('port-node-before-id',port('a@host',18446744073709551615,9),port('z@host',0,0)),('port-creation-before-id',port('a@host',9,0),port('a@host',0,9))]
contracts=[]
for name,a,b in pairs:
 body='A='+binary(a)+',B='+binary(b)+',true=A<B,true=B>A,false=A==B,false=A=:=B,true={A}<{B},true=[A]<[B],M=#{B=>2,A=>1},2=map_size(M),1=maps:get(A,M),2=maps:get(B,M),true=maps:keys(M)=:=[A,B],true={A,B}=:=binary_to_term(term_to_binary({A,B})),done'
 contracts.append((name,body))
a=pid('a@host',3,4,5)
contracts.append(('pid-equal-payload-node-creation','A='+binary(a)+',B='+binary(a)+',true=A=:=B,false=A<B,false=A>B,M=#{A=>1,B=>2},1=map_size(M),2=maps:get(A,M),done'))
lines=['namespace Erlang.Differential;','','public static class IdentifierOrderCases','{','    private const string Completion = "done";','', '    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =','    [']
for name,body in contracts:
 lines += ['        new(', '            '+json.dumps('identifier-order-'+name)+',','            '+json.dumps('-module(identifier_order_contract). -export([run/0]). run()->'+body+'.')+',','            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(Completion))','        ),']
lines+=['    ];','}','']
(out/'tools/Erlang.Differential/IdentifierOrderCases.cs').write_text('\n'.join(lines),encoding='utf8')
p=out/'tools/Erlang.Differential/CompiledModuleCases.cs';s=p.read_text();s=s.replace('.. BasedIntegerCases.Modules','.. BasedIntegerCases.Modules, .. IdentifierOrderCases.Modules');p.write_text(s)
probe=out/'tools/IdentifierOrderProbe';probe.mkdir(exist_ok=True)
(probe/'IdentifierOrderProbe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType></PropertyGroup><ItemGroup><ProjectReference Include="../Erlang.Differential/Erlang.Differential.csproj" /></ItemGroup></Project>')
(probe/'Program.cs').write_text('''using Erlang;
using Erlang.Differential;
using System.Text.Json;
var rows = new List<object>();
int failures = 0;
foreach (var fixture in IdentifierOrderCases.Modules)
{
    using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);
    Term result = await CompiledModuleExecution.Run(artifact, fixture.CallerContext);
    bool pass = TermOrder.Compare(result, fixture.Expected, true) == 0;
    if (!pass) failures++;
    rows.Add(new { fixture.Name, Pass = pass, Result = result.ToString(), Expected = fixture.Expected.ToString() });
}
Console.WriteLine(JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
return failures == 0 ? 0 : 1;
''')
print('seed',len(seed),'contracts',len(contracts),'candidate',out)
