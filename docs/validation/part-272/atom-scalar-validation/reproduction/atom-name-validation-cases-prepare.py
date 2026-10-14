from pathlib import Path
import json
p=Path('artifacts/atom-name-validation-prototype');cases=[
('supplementary-max', 'L=lists:duplicate(255,65536),A=list_to_atom(L),L=atom_to_list(A),B=atom_to_binary(A),1020=byte_size(B),A=binary_to_atom(B,utf8),A=binary_to_existing_atom(B,unicode),A=list_to_existing_atom(L)'),
('supplementary-over', 'L=lists:duplicate(256,65536),B=list_to_binary(lists:flatten(lists:duplicate(256,[240,144,128,128]))),'+','.join('ok=try '+call+' of _->unexpected catch error:'+reason+'->ok end' for call,reason in [('list_to_atom(L)','system_limit'),('list_to_existing_atom(L)','badarg'),('binary_to_atom(B,utf8)','system_limit'),('binary_to_existing_atom(B,unicode)','badarg')])),
('surrogate-list-no-pollution', 'C=erlang:system_info(atom_count),'+','.join('ok=try list_to_atom(['+str(c)+']) of _->unexpected catch error:badarg->ok end' for c in [55296,56319,56320,57343])+',C=erlang:system_info(atom_count)'),
('surrogate-utf8-no-pollution', 'C=erlang:system_info(atom_count),'+','.join('ok=try binary_to_atom(<<'+b+'>>,utf8) of _->unexpected catch error:badarg->ok end' for b in ['237,160,128','237,175,191','237,176,128','237,191,191'])+',C=erlang:system_info(atom_count)'),
('replacement-and-supplementary-distinct', 'A=list_to_atom([65533]),B=list_to_atom([65536]),false=A=:=B,M=#{A=>1,B=>2},2=map_size(M),1=maps:get(A,M),2=maps:get(B,M),A=binary_to_atom(<<239,191,189>>),B=binary_to_atom(<<240,144,128,128>>)'),
('null-and-noncharacters', 'L=[0,65534,65535,1114111],A=list_to_atom(L),L=atom_to_list(A),B=atom_to_binary(A),A=binary_to_atom(B),A=binary_to_existing_atom(B),A=list_to_existing_atom(L)'),
('list-limit-before-invalid-tail', 'L=lists:duplicate(255,97)++[55296],ok=try list_to_atom(L) of _->unexpected catch error:system_limit->ok end,ok=try list_to_existing_atom(L) of _->unexpected catch error:badarg->ok end'),
('binary-limit-before-invalid-tail', 'B=list_to_binary(lists:duplicate(255,97)++[237,160,128]),ok=try binary_to_atom(B) of _->unexpected catch error:system_limit->ok end,ok=try binary_to_existing_atom(B) of _->unexpected catch error:badarg->ok end')]
lines=['namespace Erlang.Differential;','','public static class AtomScalarValidationCases','{','    private const string Completion = "done";','','    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =','    [']
for name,body in cases:
 source='-module(atom_scalar_validation_contract). -export([run/0]). run()->'+body+',done.'
 lines+=['        new(','            '+json.dumps('atom-scalar-validation-'+name)+',','            '+json.dumps(source)+',','            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(Completion))','        ),']
lines+=['    ];','}',''];(p/'tools/Erlang.Differential/AtomScalarValidationCases.cs').write_text('\n'.join(lines))
f=p/'tools/Erlang.Differential/CompiledModuleCases.cs';s=f.read_text();needle='.. IdentifierOrderCases.Modules';assert s.count(needle)==1;s=s.replace(needle,needle+', .. AtomScalarValidationCases.Modules');f.write_text(s)
focus=p/'tools/AtomNameValidationFocus';focus.mkdir(exist_ok=True)
(focus/'AtomNameValidationFocus.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType></PropertyGroup><ItemGroup><ProjectReference Include="../Erlang.Differential/Erlang.Differential.csproj" /><Compile Include="../../tests/Erlang.Tests/AtomNameValidationChecks.cs" Link="AtomNameValidationChecks.cs" /><Compile Include="../../tests/Erlang.Tests/AtomNameValidationTestNames.cs" Link="AtomNameValidationTestNames.cs" /><Compile Include="../../tests/Erlang.Tests/AtomNameValidationTestParameters.cs" Link="AtomNameValidationTestParameters.cs" /></ItemGroup></Project>')
(focus/'Program.cs').write_text('''using Erlang;
using Erlang.Differential;
using Erlang.Tests;
using System.Text.Json;
await AtomNameValidationChecks.Run();
var rows = new List<object>();
int failures = 0;
foreach (var fixture in AtomScalarValidationCases.Modules)
{
    using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);
    Term result = await CompiledModuleExecution.Run(artifact, fixture.CallerContext);
    bool pass = result.Equals(fixture.Expected);
    if (!pass) failures++;
    rows.Add(new { fixture.Name, Pass = pass, Result = result.ToString() });
}
Console.WriteLine(JsonSerializer.Serialize(new { HostGroupPassed = true, AssemblyMvid = typeof(Atom).Assembly.ManifestModule.ModuleVersionId, Generated = rows, Failed = failures, NativeExecuted = false }, new JsonSerializerOptions { WriteIndented = true }));
return failures == 0 ? 0 : 1;
''')
print('Threeproduction+4testpaths+2differentialpaths prepared; HOST+8compiledcases independent OTPsourcecontracts')
