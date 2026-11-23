using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitAtomAndListGuardTails(FunctionDefinition function, int functionInfo)
        {
            if (!CanUseEntryTypeFacts() || function.Arity != 1 || function.Clauses is not
                [
                { Patterns: [Pattern.Variable sharedInput], Guard: { } sharedGuard, Body: Expr.Call shared } first,
                { Patterns: [Pattern.Variable atomInput], Guard: { } atomGuard, Body: Expr.Call single } second,
                {
                    Patterns: [Pattern.Variable listInput], Guard: Expr.Binary
                    {
                        Operator: ErlangOperators.AndAlso,
                        Left: Expr.Call { Arguments: [Expr.Variable checkedList] } listCheck,
                        Right: Expr.Call { Arguments: [Expr.Call { Arguments: [Expr.Variable checkedHead] } head] } integerCheck
                    }, Body: Expr.Call list
                } third
                ]
                || !HasUnconditionalParameters(first) || !HasUnconditionalParameters(second) || !HasUnconditionalParameters(third)
                || checkedList.Name != listInput.Name || checkedHead.Name != listInput.Name
                || !IsGuardBif(listCheck, AutoImportNames.IsList) || !IsGuardBif(head, AutoImportNames.Hd)
                || !IsGuardBif(integerCheck, AutoImportNames.IsInteger)
                || !owner.ResolvesErlangBif(listCheck) || !owner.ResolvesErlangBif(head) || !owner.ResolvesErlangBif(integerCheck))
            {
                return false;
            }

            var sharedKeys = new List<Atom>();
            var singleKey = new List<Atom>();
            if (!ReadAtomGuardKeys(sharedGuard, sharedInput.Name, sharedKeys)
                || sharedKeys.Count is < 2 or > MaximumFiniteAtomSetSize || sharedKeys.Distinct().Count() != sharedKeys.Count
                || !ReadAtomGuardKeys(atomGuard, atomInput.Name, singleKey) || singleKey.Count != 1
                || sharedKeys.Contains(singleKey[0]))
            {
                return false;
            }

            var atom = singleKey[0];
            var specialized = single with
            {
                Arguments = single.Arguments.Select(argument => argument is Expr.Variable variable && variable.Name == atomInput.Name
                    ? new Expr.Literal(atom)
                    : argument).ToArray()
            };
            if (ReadEntryCall(shared, [sharedInput.Name]) is not { } grouped
                || ReadEntryCall(specialized, [atomInput.Name]) is not { } exact
                || ReadEntryCall(list, [listInput.Name]) is not { } fallback)
            {
                return false;
            }

            var comparer = Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            bool singletonFirst = comparer.Compare(atom, sharedKeys.Min(comparer)!) > 0;
            int firstLabel = Label();
            int secondLabel = Label();
            int alternative = Label();
            int one = singletonFirst ? firstLabel : secondLabel;
            int many = singletonFirst ? secondLabel : firstLabel;
            var table = sharedKeys.Select(key => (Key: (Term)key, Label: many)).Append((Key: (Term)atom, Label: one));
            owner.EmitMembershipSelect(0, alternative, table.OrderBy(entry => entry.Key, comparer));

            Mark(firstLabel);
            EmitRegisterTailCall(singletonFirst ? exact : grouped);
            Mark(secondLabel);
            EmitRegisterTailCall(singletonFirst ? grouped : exact);
            Mark(alternative);

            int scratch = function.Arity;
            owner.Op(IsList, LabelTag, functionInfo);
            owner.Operand(XRegisterTag, 0);
            owner.Op(BifOne, LabelTag, functionInfo);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Hd, 1));
            owner.EmitTypedRegister(0, ProperListTypeBits);
            owner.Operand(XRegisterTag, scratch);
            owner.Op(IsInteger, LabelTag, functionInfo);
            owner.Operand(XRegisterTag, scratch);
            EmitRegisterTailCall(fallback);

            return true;
        }
    }
}
