namespace Fixture;
public class Outer
{
    public record Inner(int Value);
}
public struct Plain { }
public interface IThing { }
public enum Excluded { Value }
public delegate void ExcludedDelegate();
public partial class Repeated { }
