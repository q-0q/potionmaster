# wasp

[WIP]

Quickly create manageable, flexible state machines. Designed for games, but useful for nearly anything.

```csharp
var machine = new Wasp.Machine<State, Trigger>(State.A);

machine.Configure(State.A)
    .Permit(Trigger.X, State.B);

machine.Fire(Trigger.X);
```

Wasp is heavily inspired by [Stateless](https://github.com/dotnet-state-machine/stateless/tree/dev), and mirrors most of its patterns. Wasp adds a few features, such as

- Mutually-inclusive guard clauses
- Non-linear state inheritance
- Weakly-typed parameterized transitions
- State overriding [WIP]

Wasp currently does not support

- Asynchronous triggers
- Graph generation

## Installation

Wasp is available as a [NuGet package](www.nuget.org/packages/Wasp). For usage in Unity projects, you can use [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) to manage its installation.

##