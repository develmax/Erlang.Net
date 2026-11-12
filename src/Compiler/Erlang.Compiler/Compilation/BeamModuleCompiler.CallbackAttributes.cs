using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const string BehaviourInfoFunction = "behaviour_info";
    private const string BehaviourInfoFailureFunction = "-inlined-behaviour_info/1-";
    private const string CallbackAttribute = "callback";
    private const string OptionalCallbacksAttribute = "optional_callbacks";
    private const int BehaviourInfoLabels = 5;
    private const int BehaviourInfoFailureTarget = int.MinValue;

    private (Term Callbacks, Term Optional)? ReadCallbackInfo()
    {
        if (sourceModule is null)
        {
            return null;
        }

        var signatures = new List<Term>();
        foreach (var form in sourceModule.TypeForms.Where(form => form.Name == CallbackAttribute))
        {
            var declaration = (TupleTerm)form.Value;
            var first = (TupleTerm)((Cons)declaration.Items[1]).Head;
            signatures.Add(Term.Tuple(declaration.Items[0], Term.I(Cons.Items(first.Items[0]).Count())));
        }
        if (signatures.Count == 0)
        {
            return null;
        }
        if (sourceModule.DeclaredFunctions.Any(function => function.Name == BehaviourInfoFunction && function.Arity == 1))
        {
            throw new NotSupportedException(Unsupported);
        }

        Term optional = Cons.From(sourceModule.TypeForms.Where(form => form.Name == OptionalCallbacksAttribute)
            .SelectMany(form => Cons.Items(form.Value)).Distinct());

        return (Cons.From(signatures.Distinct()), optional);
    }

    private void EmitBehaviourInfo(Term callbacks, Term optional)
    {
        StartFunction(BehaviourInfoFunction, 1, default);
        int optionalLabel = nextLabel++;
        bool sharedReturn = callbacks.Equals(optional);
        int callbacksLabel = sharedReturn ? optionalLabel : nextLabel++;
        int failure = nextLabel++;
        Op(SelectValue, XRegisterTag, 0);
        Operand(LabelTag, failure);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 4);
        Operand(AtomTag, Intern("callbacks"));
        Operand(LabelTag, callbacksLabel);
        Operand(AtomTag, Intern(OptionalCallbacksAttribute));
        Operand(LabelTag, optionalLabel);
        Op(Label, BeamWire.UnsignedTag, optionalLabel);
        EmitConstant(optional, 0);
        instructions.WriteByte(Return);
        if (!sharedReturn)
        {
            Op(Label, BeamWire.UnsignedTag, callbacksLabel);
            EmitConstant(callbacks, 0);
            instructions.WriteByte(Return);
        }
        Op(Label, BeamWire.UnsignedTag, failure);
        Op(TailLocalCall, BeamWire.UnsignedTag, 1);
        Operand(LabelTag, BehaviourInfoFailureTarget);
    }

    private void EmitBehaviourInfoFailure()
    {
        int failure = nextLabel;
        StartFunction(BehaviourInfoFailureFunction, 1, default, exported: false);
        deferredEntries.Add(BehaviourInfoFailureTarget, failure + 1);
        Op(Jump, LabelTag, failure);
    }
}
