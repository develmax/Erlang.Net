using System.Reflection;
using Erlang;

internal sealed class ReflectedDetsFile
{
    private readonly object instance;
    private static readonly Type FileType = typeof(DetsModule).Assembly.GetType("Erlang.DetsV9File", true)!;
    private static readonly BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    internal ReflectedDetsFile(Stream stream)
    {
        instance = Activator.CreateInstance(
            FileType,
            Members,
            null,
            [stream, false, null, null],
            null
        )!;
    }

    internal Term[] Objects() => ((IEnumerable<Term>)Invoke("Objects")).ToArray();

    internal Term[] Lookup(Term key) => ((IReadOnlyList<Term>)Invoke("Lookup", key)).ToArray();

    internal bool Insert(Term[] values, bool onlyNew = false) => (bool)Invoke("Insert", values, onlyNew);

    internal void Save() => Invoke("Save", true);

    internal void Delete(Term key) => Invoke("Delete", key, null);

    private object Invoke(string method, params object?[] arguments)
    {
        try
        {
            return FileType.GetMethod(method, Members)!.Invoke(instance, arguments)!;
        }
        catch (TargetInvocationException error) when (error.InnerException is { } inner)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    internal static void Create(Stream stream, string type, Term[] values)
    {
        Assembly assembly = typeof(DetsModule).Assembly;
        object fileType = Enum.Parse(assembly.GetType("Erlang.DetsFileType", true)!, type);
        assembly.GetType("Erlang.DetsV9Writer", true)!.GetMethod("Write", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!
            .Invoke(null, [stream, values, fileType, 1, (Func<Term, uint>)ErlangPortableHash.Phash2, 256, 33554432]);
    }
}
