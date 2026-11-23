
$ErrorActionPreference='Stop'
(Get-Process -Id $PID).PriorityClass='BelowNormal'
$stage='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/frameless-tagged-fallback-1863'
$root='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts'
$out=New-Item "$stage/native-fixtures" -ItemType Directory
$cli="$out/tool"
Copy-Item -LiteralPath 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/precall-shared-frame-and-typed-lists-1861/source/tools/Build/Erlang.Tool/bin/Debug/net10.0' -Destination $cli -Recurse
Copy-Item -LiteralPath "$stage/frozen-payload/Erlang.Compiler.dll" -Destination "$cli/Erlang.Compiler.dll" -Force
$hash=(Get-Content "$stage/focused-identity.json" -Raw | ConvertFrom-Json).Compiler
if((Get-FileHash "$cli/Erlang.Compiler.dll").Hash -ne $hash){throw 'Fixture compiler is not the selected candidate'}
$env:ERL_FLAGS='+S 1:1 +SDcpu 1 +SDio 1 +A 1'
$reference=New-Item "$out/reference" -ItemType Directory
$candidate=New-Item "$out/candidate" -ItemType Directory
$trim=Get-Content "$root/local-1862-one-live-branch-frame-trim/fixture-native.eval" -Raw
$trim=$trim.Substring($trim.IndexOf('put(request,'))
$trim=$trim.Substring(0,$trim.IndexOf('{beam_file'))
$trim='[P]=init:get_plain_arguments(),{module,owned_branch_trim}=code:load_abs(filename:rootname(P)),'+$trim+'io:format("owned branch values and exception passed~n"),halt().'
$tagged='[P]=init:get_plain_arguments(),{module,leading_tag_frame_fixture}=code:load_abs(filename:rootname(P)),'+(Get-Content "$stage/../leading-tag-frame-proof-1861/check-fixture.eval" -Raw)
$receive='[P]=init:get_plain_arguments(),M=beam_shared_receive_tail,{module,M}=code:load_abs(filename:rootname(P)),R=ref,Miss={packet,other,keep},[self()!X||X<-[unrelated,{packet,R,body},{stopped,R},{failed,R},Miss]],drained=M:drain(R),{messages,[unrelated,Miss]}=process_info(self(),messages),drained=M:drain(R),{messages,[unrelated,Miss]}=process_info(self(),messages),io:format("selective receive and untouched messages passed~n"),halt().'
$numeric='[P]=init:get_plain_arguments(),M=beam_adjacent_numeric,{module,M}=code:load_abs(filename:rootname(P)),17=M:value(left,4),1=M:value(right,4),E=try M:value(error,4) of V->error({unexpected,V}) catch error:{declined,4}->declined end,io:format("~0p.~n",[{17,1,E}]),halt().'
$cases=@(
    @{Name='leading_tag_frame_fixture';Source="$stage/../leading-tag-frame-proof-1861/leading_tag_frame_fixture.erl";Eval=$tagged}
    @{Name='owned_branch_trim';Source="$root/local-1862-one-live-branch-frame-trim/owned_branch_trim.erl";Eval=$trim}
    @{Name='beam_shared_receive_tail';Source="$stage/../receive-arity-body-audit-1862/beam_shared_receive_tail.erl";Eval=$receive}
    @{Name='beam_adjacent_numeric';Source="$root/near-exact-audit-1845/local-success-types-1862/local-success-staged-fixture.erl";Eval=$numeric}
)
$receipts= @(
foreach($case in $cases){
    $name=$case.Name
    $input="$out/$name.erl"
    Copy-Item -LiteralPath $case.Source -Destination $input
    & 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed/bin/erlc.exe' -o $reference.FullName $input *> "$out/$name-reference-build.log"
    if($LASTEXITCODE){exit $LASTEXITCODE}
    & 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/sdk/dotnet.exe' "$cli/Erlang.Tool.dll" compile-beam $input "$candidate/$name.beam" *> "$out/$name-candidate-build.log"
    if($LASTEXITCODE){exit $LASTEXITCODE}
    $hashes=@(
        foreach($side in $reference,$candidate){
            $env:ERL_CRASH_DUMP="$out/$name-$($side.Name).dump"
            $output="$out/$name-$($side.Name).terms"
            & 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed/bin/erl.exe' -noshell -eval $case.Eval -extra "$side/$name.beam" > $output 2> "$out/$name-$($side.Name).stderr.log"
            if($LASTEXITCODE){exit $LASTEXITCODE}
            (Get-FileHash $output).Hash
        }
    )
    if($hashes[0] -cne $hashes[1]){throw "Native fixture observation differs: $name"}
    @{ Fixture=$name; SourceSha256=(Get-FileHash $input).Hash; ObservationSha256=$hashes[0]; Compiler=$hash}
}
)
$receipts | ConvertTo-Json | Set-Content "$out/receipt.json"

