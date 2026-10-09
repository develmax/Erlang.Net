

namespace HelloHybrid;

public interface IBox<out T>
{
    T Value
    {
        get;
    }
}
