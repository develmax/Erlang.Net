import json
from pathlib import Path
import tarfile

repo = Path(__file__).resolve().parent.parent
candidate = repo / 'artifacts/distribution-pending-runtime-prototype'
with tarfile.open(repo / 'artifacts/distribution-pending-seed.tar') as archive:
    for name in ['RuntimeReferenceResources.cs', 'ReferenceResourceKind.cs', 'ProcessRuntime.cs', 'ProcessRuntime.Halt.cs']:
        path = 'src/Erlang.Runtime/' + name
        (candidate / path).write_bytes(archive.extractfile(path).read())
resource = candidate / 'src/Erlang.Runtime/RuntimeReferenceResources.cs'
source = resource.read_text(encoding='utf-8')
assert source.count('public sealed class RuntimeReferenceResources') == 1
source = source.replace('public sealed class RuntimeReferenceResources', 'public sealed partial class RuntimeReferenceResources', 1)
resource.write_text(source, encoding='utf-8', newline='\n')
kind = candidate / 'src/Erlang.Runtime/ReferenceResourceKind.cs'
source = kind.read_text(encoding='utf-8')
assert source.count('    BinarySnapshot\n') == 1
source = source.replace('    BinarySnapshot\n', '    BinarySnapshot,\n    DistributionEntry\n', 1)
kind.write_text(source, encoding='utf-8', newline='\n')
process = candidate / 'src/Erlang.Runtime/ProcessRuntime.cs'
source = process.read_text(encoding='utf-8')
assert source.count('                ProcessTracing.Terminated(ctx.Self);') == 1
source = source.replace('                ProcessTracing.Terminated(ctx.Self);', '                distributionPending?.ProcessTerminated(ctx);\n                ProcessTracing.Terminated(ctx.Self);', 1)
assert source.count('            ReferenceResources.Close();') == 2
source = source.replace('            ReferenceResources.Close();', '            distributionPending?.Close();\n            ReferenceResources.Close();')
# The ordinary DisposeAsync block has an additional indentation level.
source = source.replace('                distributionPending?.Close();\n            ReferenceResources.Close();', '                distributionPending?.Close();\n                ReferenceResources.Close();')
process.write_text(source, encoding='utf-8', newline='\n')
halt = candidate / 'src/Erlang.Runtime/ProcessRuntime.Halt.cs'
source = halt.read_text(encoding='utf-8')
assert source.count('            ReferenceResources.Close();') == 1
source = source.replace('            ReferenceResources.Close();', '            distributionPending?.Close();\n            ReferenceResources.Close();', 1)
halt.write_text(source, encoding='utf-8', newline='\n')
print(json.dumps({'Candidate': str(candidate), 'ExistingFilesChanged': 4, 'CoreRegistrationsAdded': 0}))
