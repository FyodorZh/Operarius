## Operarius

A small C# library providing a framework for scheduling and running periodic business logic with multiple driver strategies and time utilities. Designed for high-control, testable periodic execution with simple driver/runnable abstractions.

### Key concepts
- Periodic logic implements `IPeriodicLogic` and receives lifecycle callbacks: `LogicStarted`, repeated `LogicTick`, and `LogicStopped`.
- Drivers (`IPeriodicLogicDriver`) take responsibility for ticking logic at a configured `DeltaTime` period.
- Runners (`IPeriodicLogicRunner`) start logic using an internal driver and return a control handle (`ILogicDriverCtl`) to manage the running instance.
