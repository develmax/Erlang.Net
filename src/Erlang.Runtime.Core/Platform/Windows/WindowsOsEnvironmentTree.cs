namespace Erlang;

// Bottom-up insertion, successor splicing and black-null sentinel deletion
// follow pinned erl_rbtree.h. Enumeration is left/right/node postorder.
internal sealed class WindowsOsEnvironmentTree(WindowsOsEnvironmentLocale locale)
{
    private sealed class Node(string key, string value)
    {
        internal string Key = key, Value = value;
        internal Node? Parent, Left, Right;
        internal bool Red;
    }
    private Node? root;

    internal void ClearDriverRoots422() => root = null;

    internal string? Get(string key) => Find(key)?.Value;

    private Node? Find(string key)
    {
        Node? node = root;
        while (node is not null)
        {
            int comparison = locale.Compare(key, node.Key);
            if (comparison == 0) return node;
            node = comparison < 0 ? node.Left : node.Right;
        }

        return null;
    }

    internal void Put(string key, string value)
    {
        Node? parent = null, node = root;
        int comparison = 0;
        while (node is not null)
        {
            parent = node;
            comparison = locale.Compare(key, node.Key);
            if (comparison == 0)
            {
                node.Key = key;
                node.Value = value;

                return;
            }
            node = comparison < 0 ? node.Left : node.Right;
        }
        var fresh = new Node(key, value) { Parent = parent, Red = parent is not null };
        if (parent is null)
        {
            root = fresh;

            return;
        }
        if (comparison < 0) parent.Left = fresh; else parent.Right = fresh;
        while (fresh.Parent is { Red: true } p)
        {
            Node grand = p.Parent!;
            bool left = ReferenceEquals(p, grand.Left);
            Node? uncle = left ? grand.Right : grand.Left;
            if (uncle is { Red: true })
            {
                uncle.Red = false;
                p.Red = false;
                grand.Red = true;
                fresh = grand;
                if (ReferenceEquals(fresh, root)) break;
            }
            else
            {
                if (left && ReferenceEquals(fresh, p.Right))
                {
                    fresh = p;
                    RotateLeft(fresh);
                    p = fresh.Parent!;
                    grand = p.Parent!;
                }
                else if (!left && ReferenceEquals(fresh, p.Left))
                {
                    fresh = p;
                    RotateRight(fresh);
                    p = fresh.Parent!;
                    grand = p.Parent!;
                }
                p.Red = false;
                grand.Red = true;
                if (left) RotateRight(grand); else RotateLeft(grand);
                break;
            }
        }
        root!.Red = false;
    }

    private void Replace(Node old, Node replacement)
    {
        Node? parent = old.Parent;
        if (parent is null) root = replacement;
        else if (ReferenceEquals(old, parent.Left)) parent.Left = replacement;
        else parent.Right = replacement;
        replacement.Parent = parent;
        replacement.Left = old.Left;
        replacement.Right = old.Right;
        replacement.Red = old.Red;
        if (replacement.Left is not null) replacement.Left.Parent = replacement;
        if (replacement.Right is not null) replacement.Right.Parent = replacement;
    }

    internal bool Remove(string key)
    {
        Node? z = Find(key);
        if (z is null) return false;
        Node y = z;
        if (z.Left is not null && z.Right is not null)
        {
            y = z.Right;
            while (y.Left is not null) y = y.Left;
        }
        bool black = !y.Red;
        Node? x = y.Left ?? y.Right;
        Node? parent = y.Parent;
        Node? sentinel = null;
        if (x is not null) x.Parent = parent;
        else if (black)
        {
            x = sentinel = new Node(string.Empty, string.Empty) { Parent = parent };
            y.Left = x;
        }
        if (parent is null) root = x;
        else if (ReferenceEquals(y, parent.Left)) parent.Left = x;
        else parent.Right = x;
        if (!ReferenceEquals(y, z)) Replace(z, y);
        if (black)
        {
            FixDelete(x!);
            if (sentinel is not null)
            {
                parent = sentinel.Parent;
                if (parent is null)
                {
                    if (ReferenceEquals(root, sentinel)) root = null;
                }
                else if (ReferenceEquals(parent.Left, sentinel)) parent.Left = null;
                else if (ReferenceEquals(parent.Right, sentinel)) parent.Right = null;
            }
        }

        return true;
    }

    private void FixDelete(Node x)
    {
        while (!x.Red && x.Parent is { } parent)
        {
            bool left = ReferenceEquals(x, parent.Left);
            Node sibling = (left ? parent.Right : parent.Left)!;
            if (sibling.Red)
            {
                sibling.Red = false;
                parent.Red = true;
                if (left) RotateLeft(parent); else RotateRight(parent);
                parent = x.Parent!;
                sibling = (left ? parent.Right : parent.Left)!;
            }
            Node? near = left ? sibling.Left : sibling.Right;
            Node? far = left ? sibling.Right : sibling.Left;
            if (near is not { Red: true } && far is not { Red: true })
            {
                sibling.Red = true;
                x = parent;
            }
            else
            {
                if (far is not { Red: true })
                {
                    near!.Red = false;
                    sibling.Red = true;
                    if (left) RotateRight(sibling); else RotateLeft(sibling);
                    parent = x.Parent!;
                    sibling = (left ? parent.Right : parent.Left)!;
                }
                sibling.Red = parent.Red;
                parent.Red = false;
                (left ? sibling.Right : sibling.Left)!.Red = false;
                if (left) RotateLeft(parent); else RotateRight(parent);
                x = root!;
                break;
            }
        }
        x.Red = false;
    }

    private void RotateLeft(Node x)
    {
        Node y = x.Right!;
        x.Right = y.Left;
        if (y.Left is not null) y.Left.Parent = x;
        y.Parent = x.Parent;
        if (x.Parent is null) root = y;
        else if (ReferenceEquals(x, x.Parent.Left)) x.Parent.Left = y; else x.Parent.Right = y;
        y.Left = x;
        x.Parent = y;
    }

    private void RotateRight(Node x)
    {
        Node y = x.Left!;
        x.Left = y.Right;
        if (y.Right is not null) y.Right.Parent = x;
        y.Parent = x.Parent;
        if (x.Parent is null) root = y;
        else if (ReferenceEquals(x, x.Parent.Right)) x.Parent.Right = y; else x.Parent.Left = y;
        y.Right = x;
        x.Parent = y;
    }

    internal IReadOnlyList<KeyValuePair<string, string>> PostOrder(CancellationToken cancellation = default)
    {
        var result = new List<KeyValuePair<string, string>>();
        var pending = new Stack<(Node Node, bool Visit)>();
        if (root is not null) pending.Push((root, false));
        while (pending.TryPop(out var item))
        {
            cancellation.ThrowIfCancellationRequested();
            if (item.Visit) result.Add(new(item.Node.Key, item.Node.Value));
            else
            {
                pending.Push((item.Node, true));
                if (item.Node.Right is not null) pending.Push((item.Node.Right, false));
                if (item.Node.Left is not null) pending.Push((item.Node.Left, false));
            }
        }

        return result.AsReadOnly();
    }
}
