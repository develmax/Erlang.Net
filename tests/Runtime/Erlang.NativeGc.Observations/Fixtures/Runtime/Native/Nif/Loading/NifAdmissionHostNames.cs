using System.Reflection;

namespace Erlang.Tests;

internal static class NifAdmissionHostNames
{
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic;
    internal const string Test = "runtime/nif-caller-image-reservation-generation-and-cleanup";
    internal const string Module = "nif_admission_host";
    internal const string Function = "fallback";
    internal const string Reserve = "ReserveNifImageAsync";
    internal const string Initialize = "InitializeAndCommitAsync";
    internal const string InitializedType = "Erlang.NifImageInitialized";
    internal const string FactoryMethod = "MakeInitializer";
    internal const string AsTask = "AsTask";
    internal const string Result = "Result";
    internal const string Failure = "Failure";
    internal const string Origin = "Origin";
    internal const string Current = "Current";
    internal const string Pending = "PendingOnLoad";
    internal const string Image = "Image";
    internal const string CodeImage = "CodeImage";
    internal const string NifBindings = "NifBindings";
    internal const string Retire = "RetireAsync";
    internal const string Permission = "CodeModification";
    internal const string Waiting = "PendingSynchronousWriters";
    internal const string Gate = "gate";
    internal const string Descriptors = "codeDescriptors";
    internal const string NoCaller = "NoExecutingCaller";
    internal const string WrongRuntime = "ForeignRuntime";
    internal const string OldCode = "OldCode";
    internal const string Abandoned = "ReservationAbandoned";
    internal const string Changed = "GenerationChanged";
    internal const string Reload = "Reload";
    internal const string Upgrade = "UpgradeUnsupported";
    internal const string Ambiguous = "PendingCurrentAmbiguous";
    internal const string Reentry = "ReentrantInitializationOperation";
    internal const string Other = "nif_admission_other";
    internal const string Missing = "missing";
    internal const string ActorModule = "nif_admission_actor_other";
    internal const string DeleteModule = "delete_module";
    internal const string PendingModule = "beam_on_load_fresh_success";
    internal const string Extension = ".beam";
    internal const string Descriptor = "Descriptor";
    internal const string ModuleInfo = "module_info";
    internal const string Broken = "NIF caller image admission contract failed";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
