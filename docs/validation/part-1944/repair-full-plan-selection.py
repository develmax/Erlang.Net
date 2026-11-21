from pathlib import Path
import sys
for p in map(Path,sys.argv[1:]):
    s=p.read_text(encoding='utf-8-sig')
    old="""    & 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' "$s/replan-from-parent-times.py" "$p/full-uncached" "$s/replanned-five-input" > "$s/replan.stdout.log" 2> "$s/replan.stderr.log"
    if($LASTEXITCODE){exit $LASTEXITCODE}
    $model=Get-Content "$s/replanned-five-input/planning-receipt.json" -Raw | ConvertFrom-Json
    if($model.ExpectedStragglerReduction -ge 0.03){$moduleDirectory="$s/replanned-five-input"}
    @{ExpectedReduction=$model.ExpectedStragglerReduction;MeasuredParent=$p;Selected=$moduleDirectory} |
"""
    new="""    $receipt="$s/replanned-five-input/planning-receipt.json"
    if(!(Test-Path -LiteralPath $receipt)){
        & 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' "$s/replan-from-parent-times.py" "$p/full-uncached" "$s/replanned-five-input" > "$s/replan.stdout.log" 2> "$s/replan.stderr.log"
        if($LASTEXITCODE){exit $LASTEXITCODE}
    }
    $model=Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json
    if((Get-FileHash "$p/full-uncached/timing.json").Hash -ne $model.Sources.OldFullTimingSha256
        -or (Get-FileHash "$p/full-uncached/otp-corpus.json").Hash -ne $model.Sources.OldCorpusSha256
        -or $model.count -ne 1366 -or $model.Workers -ne 5 -or $model.CompileResultsReused){
        throw 'Measured partition source or dimensions differ'
    }
    $reason='Preserved existing baseline partition'
    if(!(Test-Path -LiteralPath "$moduleDirectory/planning-receipt.json") -or $model.ExpectedStragglerReduction -ge 0.03){
        $reason=if(Test-Path -LiteralPath "$moduleDirectory/planning-receipt.json"){'Measured expected improvement'}else{'Only available complete measured partition'}
        $moduleDirectory="$s/replanned-five-input"
    }
    @{ExpectedReduction=$model.ExpectedStragglerReduction;MeasuredParent=$p;Selected=$moduleDirectory;Reason=$reason} |
"""
    assert s.count(old)==1,p
    p.with_name(p.stem+'-initial.ps1').write_bytes(p.read_bytes())
    p.write_text(s.replace(old,new),encoding='utf-8',newline='\n')

