namespace Automatonymous;

/// <summary>Identifies a state in a state-machine definition.</summary>
public sealed class State
{
    internal State(string name) => Name = name;

    /// <summary>Gets the declaration name of the state.</summary>
    public string Name { get; }

    /// <inheritdoc />
    public override string ToString() => Name;
}
