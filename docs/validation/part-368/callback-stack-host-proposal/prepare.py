import shutil
from pathlib import Path

OUT=Path(__file__).resolve().parent
OLD=OUT.parent/'queue-callback368-regression-proposal1'
shutil.copytree(OLD/'source',OUT/'source')
checks=OUT/'source/tests/Erlang.Tests/QueueCallbackRegressionHostChecks.cs'
source=checks.read_text().replace('OracleOutcomeTags.Success','QueueCallbackRegressionNames.Success')
source=source.replace('ListModuleNames.Module','QueueCallbackRegressionNames.ListsModule')
source=source.replace('ListModuleNames.Reverse','QueueCallbackRegressionNames.ListReverse')
source=source.replace('ListModuleNames.Any','QueueModuleNames.Any').replace('ListModuleNames.All','QueueModuleNames.All')
source=source.replace('identity.Name == QueueCallbackRegressionNames.Wrong','identity.Name?.Name == QueueCallbackRegressionNames.Wrong')
checks.write_text(source,encoding='utf-8')
names=OUT/'source/tests/Erlang.Tests/QueueCallbackRegressionNames.cs'
source=names.read_text().replace('    internal const string Test =', '    internal const string Success = "ok";\n    internal const string ListsModule = "lists";\n    internal const string ListReverse = "reverse";\n    internal const string Test =')
names.write_text(source,encoding='utf-8')
seal=(OLD/'seal.py').read_text().replace('This proposal is source-only and unbuilt/unrun;', 'Successor2 repairs test-only access to internal list/outcome constants and reads actual LocalFunctionIdentity.Name as Atom.Name; frozen proposal1 is unchanged. This proposal is source-only and unbuilt/unrun;')
(OUT/'seal.py').write_text(seal,encoding='utf-8')
