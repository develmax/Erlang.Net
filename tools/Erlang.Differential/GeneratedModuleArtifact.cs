using System.Runtime.Loader;
using Erlang.Compiler;

namespace Erlang.Differential;

public sealed class GeneratedModuleArtifact : IDisposable
{
    private readonly AssemblyLoadContext context;

    public ModuleDefinition Definition { get; }
    public string CSharpSha256 { get; }

    internal GeneratedModuleArtifact(AssemblyLoadContext context, ModuleDefinition definition, string hash)
    {
        this.context = context;
        Definition = definition;
        CSharpSha256 = hash;
    }

    public void Dispose() => context.Unload();
}
