{
  "library": "Operarius",
  "meta": {
    "target": "netstandard2.1",
    "lang": 9,
    "nullable": true
  },
  "philosophy": "Operarius is a task-execution framework that splits 'logic' (what to do: ILogic lifecycle callbacks) from 'driver' (when/how often to invoke it: periodic, semi-periodic with self-chosen delay, or on-demand). Drivers own the clock (via injectable IDateTimeProvider) and thread model, so logic stays pure and testable; manual drivers let you tick deterministically from tests, while thread-based multi drivers self-drive many logic instances at once.",
  "quickPick": [
    {
      "need": "Run several fixed-period logic instances on their own background thread",
      "use": "ThreadBasedPeriodicMultiLogicDriver"
    },
    {
      "need": "Schedule multiple logic instances each choosing its own next delay",
      "use": "ThreadBasedSemiPeriodicMultiLogicDriver"
    },
    {
      "need": "Run logic only when explicitly requested (event-driven)",
      "use": "ThreadBasedNonPeriodicLogicMultiDriver"
    },
    {
      "need": "Deterministic fixed-period ticking controlled from a test",
      "use": "ManualPeriodicLogicDriver"
    },
    {
      "need": "Deterministic self-delaying logic controlled from a test",
      "use": "ManualSemiPeriodicLogicDriver"
    },
    {
      "need": "Guarantee at most one logic instance is ever driven",
      "use": "SingleJobLogicDriver"
    },
    {
      "need": "Controllable fake clock for time-dependent tests",
      "use": "ThreadSafeDateTime"
    },
    {
      "need": "Current UTC time provider for thread drivers",
      "use": "UtcNowDateTimeProvider"
    },
    {
      "need": "Debug-time invariant checking of the start/tick/stop state machine",
      "use": "PeriodicLogicChecker"
    }
  ],
  "decisionTree": [
    [
      "Need to drive ticks yourself (deterministic tests / no background thread)?",
      "yes",
      "ManualPeriodicLogicDriver"
    ],
    [
      "Need manual ticking and each tick decides the next delay itself?",
      "yes",
      "ManualSemiPeriodicLogicDriver"
    ],
    [
      "Fixed period with automatic background scheduling?",
      "yes",
      "ThreadBasedPeriodicMultiLogicDriver"
    ],
    [
      "Each logic instance sets its own interval, auto-scheduled?",
      "yes",
      "ThreadBasedSemiPeriodicMultiLogicDriver"
    ],
    [
      "Invoke logic only when something happens (on demand)?",
      "yes",
      "ThreadBasedNonPeriodicLogicMultiDriver"
    ],
    [
      "Must never run two logic instances concurrently?",
      "yes",
      "SingleJobLogicDriver"
    ],
    [
      "Need to control/mock the current time?",
      "yes",
      "ThreadSafeDateTime"
    ]
  ],
  "interfaces": [
    {
      "name": "ILogic<in TLogicDriverCtl>",
      "variance": "in TLogicDriverCtl",
      "genericConstraints": "where TLogicDriverCtl : ILogicDriverCtl",
      "members": [
        {
          "sig": "LogicStarted(TLogicDriverCtl driver):bool",
          "desc": "Called once by the driver before any LogicTick to initialize the logic. Returns false if initialization failed; in that case LogicStopped is invoked immediately after and Start reports FailedToStart. Called on the driver's thread. Complexity O(1).",
          "threadSafety": "called on the driver's thread; not safe for concurrent calls",
          "preconditions": [
            "driver is the driver control that will be used for this run"
          ],
          "postconditions": [
            "returns true to signal a running logic, false to abort startup"
          ],
          "example": "return _store.Init(driver.CurrentTime);",
          "remarks": "Keep this fast and non-blocking; the driver thread waits for it."
        },
        {
          "sig": "LogicStopped()",
          "desc": "Invoked once when the logic is terminated (graceful driver.Stop, failure, or failed start) for cleanup and resource deallocation. Must not throw; if it does, the driver reports it via ErrorStream and continues. Called on the driver's thread.",
          "threadSafety": "called on the driver's thread; not safe for concurrent calls",
          "preconditions": [
            "LogicStarted has been invoked"
          ],
          "postconditions": [
            "logic is cleaned up; no further LogicTick calls follow"
          ]
        }
      ]
    },
    {
      "name": "ILogicDriverCtl",
      "members": [
        {
          "sig": "CurrentTime:DateTime { get; }",
          "desc": "The driver's current clock value (from the injected IDateTimeProvider). Read-only view of time for the running logic.",
          "threadSafety": "safe to read from any thread",
          "example": "var now = driver.CurrentTime;"
        },
        {
          "sig": "Stop()",
          "desc": "Requests the driver to stop executing this logic. Asynchronous intent: the logic's LogicStopped is invoked and the driver's LogicStopped event fires shortly after. Called from within a tick it takes effect at the end of the current tick cycle.",
          "threadSafety": "safe to call from any thread",
          "example": "driver.Stop();"
        }
      ]
    },
    {
      "name": "ILogicDriver<TLogicDriverCtl>",
      "genericConstraints": "where TLogicDriverCtl : ILogicDriverCtl",
      "members": [
        {
          "sig": "event Action<ILogic<TLogicDriverCtl>>? LogicStopped",
          "desc": "Raised when a started logic terminates (natural stop via Stop(), failed start, or exception on tick/stop). Handler receives the logic instance that ended. Raised exactly once per successful start. Exceptions thrown by handlers are swallowed by drivers.",
          "example": "driver.LogicStopped += l => Console.WriteLine(\"stopped\");"
        },
        {
          "sig": "event Action<Exception>? ErrorStream",
          "desc": "Raised for exceptions that occur inside logic callbacks (LogicStarted, LogicTick, LogicStopped) or driver machinery. Subscribing is optional but recommended; unhandled logic exceptions never propagate to the caller — they end up here.",
          "example": "driver.ErrorStream += ex => Log(ex);"
        },
        {
          "sig": "Start(ILogic<TLogicDriverCtl> logic):LogicStartResult",
          "desc": "Starts the given logic on this driver. Returns Success on success; other values indicate the logic was not started (see LogicStartResult). If the logic's LogicStarted returns false or throws, the driver invokes LogicStopped, raises LogicStopped, and returns FailedToStart.",
          "preconditions": [
            "logic is not null",
            "logic has not already been started on this driver"
          ],
          "postconditions": [
            "on Success: LogicStarted was called and returned true; ticks may follow",
            "on failure: LogicStarted and LogicStopped were both invoked, LogicStopped event was raised"
          ],
          "example": "var res = driver.Start(logic);",
          "remarks": "Ownership: the driver holds a strong reference to the logic until it stops."
        },
        {
          "sig": "Finish():Task",
          "desc": "Requests graceful shutdown of the driver and returns a Task that completes when the driver has fully finished (all logics stopped). Manual drivers throw NotSupportedException instead. May be called multiple times; the returned task is shared.",
          "threadSafety": "safe to call from any thread",
          "example": "await driver.Finish();"
        },
        {
          "sig": "WaitForFinish():Task",
          "desc": "Returns a Task that completes when the driver has finished. Unlike Finish it does not request shutdown; use it to await an already-finishing driver. Manual drivers throw NotSupportedException.",
          "threadSafety": "safe to call from any thread",
          "example": "await driver.WaitForFinish();"
        }
      ]
    },
    {
      "name": "INonPeriodicLogic",
      "extends": [
        "ILogic<INonPeriodicLogicDriverCtl>"
      ],
      "members": [
        {
          "sig": "LogicTick(INonPeriodicLogicDriverCtl driver)",
          "desc": "Invoked each time the driver grants an invocation quantum (after successful LogicStarted, until LogicStopped). The logic requests further invocations via driver.RequestInvocation(). Ticks never overlap. Complexity O(1) contract.",
          "preconditions": [
            "LogicStarted returned true"
          ],
          "postconditions": [
            "logic should call driver.RequestInvocation() to be ticked again"
          ],
          "example": "void INonPeriodicLogic.LogicTick(INonPeriodicLogicDriverCtl d) { Consume(); d.RequestInvocation(); }"
        }
      ]
    },
    {
      "name": "INonPeriodicLogicDriverCtl",
      "extends": [
        "ILogicDriverCtl"
      ],
      "members": [
        {
          "sig": "RequestInvocation()",
          "desc": "Asynchronously requests an out-of-queue tick quantum for the logic. Non-blocking; the actual LogicTick runs on the driver's background thread, never inline. Consecutive requests while a tick is pending are coalesced.",
          "threadSafety": "safe to call from any thread",
          "example": "driver.RequestInvocation();"
        }
      ]
    },
    {
      "name": "IPeriodicLogic",
      "extends": [
        "ILogic<IPeriodicLogicDriverCtl>"
      ],
      "members": [
        {
          "sig": "LogicTick(IPeriodicLogicDriverCtl driver)",
          "desc": "Invoked at a fixed period while the logic is running (after successful LogicStarted, until LogicStopped). The period is fixed by the driver; call driver.Stop() from a tick to end execution. Ticks never overlap. Complexity O(1) contract.",
          "preconditions": [
            "LogicStarted returned true"
          ],
          "example": "void IPeriodicLogic.LogicTick(IPeriodicLogicDriverCtl d) { PollSensor(); }"
        }
      ]
    },
    {
      "name": "IPeriodicLogicDriverCtl",
      "extends": [
        "ILogicDriverCtl"
      ],
      "members": [
        {
          "sig": "Period:TimeSpan { get; }",
          "desc": "The fixed tick period configured on the driver. Read-only; used by logic to reason about time between ticks.",
          "example": "var period = driver.Period;"
        }
      ]
    },
    {
      "name": "ISemiPeriodicLogic",
      "extends": [
        "ILogic<ISemiPeriodicLogicDriverCtl>"
      ],
      "members": [
        {
          "sig": "LogicTick(ISemiPeriodicLogicDriverCtl driver):TimeSpan",
          "desc": "Invoked while the logic is running; the returned TimeSpan is the delay until the next tick. Must return a non-negative value — a negative value makes ManualSemiPeriodicLogicDriver throw InvalidOperationException. Ticks never overlap. Complexity O(1) contract.",
          "preconditions": [
            "LogicStarted returned true"
          ],
          "postconditions": [
            "returns the delay until the next tick"
          ],
          "example": "TimeSpan ISemiPeriodicLogic.LogicTick(ISemiPeriodicLogicDriverCtl d) { Poll(); return TimeSpan.FromMilliseconds(50); }"
        }
      ]
    },
    {
      "name": "ISemiPeriodicLogicDriverCtl",
      "extends": [
        "ILogicDriverCtl"
      ],
      "desc": "Empty marker interface; the tick interval is not exposed here but chosen by the logic's return value from ISemiPeriodicLogic.LogicTick."
    },
    {
      "name": "IDateTimeProvider",
      "members": [
        {
          "sig": "Now:DateTime { get; }",
          "desc": "The current time as seen by the driver. Swap in a fake (ThreadSafeDateTime) for deterministic tests. Stateless and thread-safe in the built-in implementations.",
          "threadSafety": "thread-safe",
          "example": "var now = provider.Now;"
        }
      ]
    },
    {
      "name": "IUtcDateTimeProvider",
      "extends": [
        "IDateTimeProvider"
      ],
      "desc": "Marker interface for providers that always return UTC time (see UtcNowDateTimeProvider.Instance)."
    }
  ],
  "types": [
    {
      "name": "LogicStartResult",
      "kind": "enum",
      "isAuxiliary": true,
      "desc": "Success=0, FailedToStart=1, CapacityExceeded=2, DriverIsNotActive=3"
    },
    {
      "name": "PeriodicLogicChecker",
      "kind": "staticClass",
      "category": "utility",
      "isAuxiliary": true,
      "desc": "Static holder of the Debug-only invariant-checking extension Test (see extensions for IPeriodicLogic)."
    },
    {
      "name": "UtcNowDateTimeProvider",
      "kind": "class",
      "category": "time-provider",
      "base": "object",
      "implements": [
        "IUtcDateTimeProvider"
      ],
      "desc": "Singleton provider returning DateTime.UtcNow. Private constructor; access via Instance. Stateless, thread-safe.",
      "threadSafety": "thread-safe (stateless)",
      "properties": [
        {
          "sig": "static Instance:IDateTimeProvider { get; }",
          "desc": "The shared singleton instance. Use this as the IDateTimeProvider argument for thread-based drivers when you want real UTC time.",
          "example": "var driver = new ThreadBasedPeriodicMultiLogicDriver(UtcNowDateTimeProvider.Instance, TimeSpan.FromMilliseconds(10));"
        },
        {
          "sig": "Now:DateTime { get; }",
          "desc": "Returns DateTime.UtcNow.",
          "threadSafety": "thread-safe",
          "example": "var utc = UtcNowDateTimeProvider.Instance.Now;"
        }
      ]
    },
    {
      "name": "NowDateTimeProvider",
      "kind": "class",
      "category": "time-provider",
      "base": "object",
      "implements": [
        "IDateTimeProvider"
      ],
      "desc": "Singleton provider returning DateTime.Now (local time). Private constructor; access via Instance. Stateless, thread-safe.",
      "threadSafety": "thread-safe (stateless)",
      "properties": [
        {
          "sig": "static Instance:IDateTimeProvider { get; }",
          "desc": "The shared singleton instance returning local time. Prefer UtcNowDateTimeProvider unless you explicitly need local wall-clock.",
          "example": "var driver = new ThreadBasedNonPeriodicLogicMultiDriver(NowDateTimeProvider.Instance);"
        },
        {
          "sig": "Now:DateTime { get; }",
          "desc": "Returns DateTime.Now.",
          "threadSafety": "thread-safe",
          "example": "var local = NowDateTimeProvider.Instance.Now;"
        }
      ]
    },
    {
      "name": "ThreadSafeDateTime",
      "kind": "class",
      "category": "time-provider",
      "base": "object",
      "implements": [
        "IDateTimeProvider"
      ],
      "desc": "Mutable, thread-safe DateTime holder backed by Interlocked on the 64-bit binary representation. Use as a fake clock: pass it to a thread-based driver and mutate Time from tests.",
      "threadSafety": "thread-safe (Interlocked)",
      "constructors": [
        {
          "sig": "ctor()",
          "desc": "Creates a holder initialized to default(DateTime) (DateTime.MinValue).",
          "example": "var clock = new ThreadSafeDateTime();"
        },
        {
          "sig": "ctor(DateTime time)",
          "desc": "Creates a holder initialized to the given time.",
          "example": "var clock = new ThreadSafeDateTime(DateTime.UtcNow);"
        }
      ],
      "properties": [
        {
          "sig": "Time:DateTime { get; set; }",
          "desc": "Gets or sets the held time using Interlocked (exchange + atomic 64-bit read). Safe for concurrent readers/writers; last writer wins.",
          "threadSafety": "thread-safe",
          "example": "clock.Time = clock.Time.AddSeconds(1);"
        },
        {
          "sig": "Now:DateTime { get; }",
          "desc": "Returns Time; satisfies IDateTimeProvider so the holder can be injected into drivers.",
          "threadSafety": "thread-safe",
          "example": "var now = clock.Now;"
        }
      ]
    },
    {
      "name": "SingleJobLogicDriver<TLogicDriverCtl>",
      "kind": "class",
      "category": "adapter",
      "base": "object",
      "implements": [
        "ILogicDriver<TLogicDriverCtl>"
      ],
      "genericConstraints": "where TLogicDriverCtl : ILogicDriverCtl",
      "desc": "Adapter wrapping a core driver and allowing at most one Start ever. After the wrapped logic stops, the gate is not reset, so any further Start returns CapacityExceeded. Forwards LogicStopped/ErrorStream and drives Finish on the core.",
      "threadSafety": "Start is thread-safe (Interlocked gate); events forwarded from the core driver",
      "limitations": "Not reusable — a new instance is required for a second logic run.",
      "constructors": [
        {
          "sig": "ctor(ILogicDriver<TLogicDriverCtl> coreDriver)",
          "desc": "Wraps coreDriver; subscribes to its LogicStopped and ErrorStream events to forward them and to trigger Finish.",
          "preconditions": [
            "coreDriver is not null"
          ],
          "example": "var driver = new SingleJobLogicDriver<IPeriodicLogicDriverCtl>(new ThreadBasedPeriodicMultiLogicDriver(UtcNowDateTimeProvider.Instance, TimeSpan.FromMilliseconds(10)));"
        }
      ],
      "properties": [],
      "methods": [
        {
          "sig": "Start(ILogic<TLogicDriverCtl> logic):LogicStartResult",
          "desc": "Starts logic on the core driver if this adapter has never started anything; otherwise returns CapacityExceeded. The internal gate is never reset after the logic stops.",
          "threadSafety": "thread-safe",
          "preconditions": [
            "logic is not null"
          ],
          "postconditions": [
            "first call delegates to the core driver and returns its result",
            "any later call returns CapacityExceeded"
          ],
          "example": "var res = singleJobDriver.Start(logic);"
        },
        {
          "sig": "Finish():Task",
          "desc": "Forwards to the core driver's Finish. Also called internally when the wrapped logic stops (with task exceptions forwarded to ErrorStream).",
          "threadSafety": "thread-safe",
          "example": "await singleJobDriver.Finish();"
        },
        {
          "sig": "WaitForFinish():Task",
          "desc": "Forwards to the core driver's WaitForFinish.",
          "threadSafety": "thread-safe",
          "example": "await singleJobDriver.WaitForFinish();"
        }
      ],
      "events": [
        {
          "sig": "event Action<ILogic<TLogicDriverCtl>>? LogicStopped",
          "desc": "Raised when the wrapped logic stops (forwarded from the core driver)."
        },
        {
          "sig": "event Action<Exception>? ErrorStream",
          "desc": "Raised for errors from the core driver or from Finish continuations."
        }
      ]
    },
    {
      "name": "PeriodicLikeLogicManualDriver<TLogicDriverCtl>",
      "kind": "class",
      "category": "driver-manual",
      "base": "object",
      "implements": [
        "ILogicDriver<TLogicDriverCtl>",
        "ILogicDriverCtl"
      ],
      "genericConstraints": "where TLogicDriverCtl : class, ILogicDriverCtl",
      "desc": "Abstract base for drivers ticked externally: you call Tick(now) (or StopNow) and the driver decides whether a tick is due by comparing now against NextTickTime. Holds at most one logic. Finish/WaitForFinish throw NotSupportedException. Implements ILogicDriverCtl so the running logic can Stop() it.",
      "threadSafety": "designed for a single external ticking thread; Stop/StopNow may be called from another thread (volatile stop flag, Interlocked start)",
      "limitations": "Use only when you control the clock (tests, custom scheduler). For automatic scheduling use the thread-based drivers.",
      "properties": [
        {
          "sig": "NextTickTime:DateTime { get; }",
          "desc": "The scheduled time of the next tick. A tick fires when CurrentTime >= NextTickTime. Updated by InvokeTick implementations (periodic: += period; semi: += returned delay).",
          "example": "var next = driver.NextTickTime;"
        },
        {
          "sig": "CurrentTime:DateTime { get; }",
          "desc": "The last time passed to Tick(DateTime). Serves as the driver control's clock view.",
          "example": "var now = driver.CurrentTime;"
        },
        {
          "sig": "IsRunning:bool { get; }",
          "desc": "True while a logic is attached (volatile read of the logic reference).",
          "threadSafety": "thread-safe (volatile read)",
          "example": "if (driver.IsRunning) { }"
        }
      ],
      "methods": [
        {
          "sig": "protected abstract InvokeStart(ILogic<TLogicDriverCtl> logic):bool",
          "desc": "Subclasses call logic.LogicStarted(this) and return its result."
        },
        {
          "sig": "protected abstract InvokeTick(ILogic<TLogicDriverCtl> logic)",
          "desc": "Subclasses invoke the concrete logic's LogicTick and advance NextTickTime. Exceptions propagate to DoTick, which reports them and stops the logic."
        },
        {
          "sig": "Start(ILogic<TLogicDriverCtl> logic):LogicStartResult",
          "desc": "Attaches the logic (CapacityExceeded if one is already running). Calls InvokeStart; on false or on an exception it invokes LogicStopped, raises the LogicStopped event, detaches, and returns FailedToStart. Exceptions from LogicStopped itself are reported to ErrorStream.",
          "preconditions": [
            "logic is not null"
          ],
          "postconditions": [
            "on Success the logic is attached and IsRunning is true",
            "on failure LogicStopped event is raised exactly once"
          ],
          "example": "var res = driver.Start(logic);"
        },
        {
          "sig": "Tick():bool",
          "desc": "Ticks using the driver's own NextTickTime as the clock. Returns true while still running, false when stopped or idle. Equivalent to Tick(_nextTickTime).",
          "example": "bool running = driver.Tick();"
        },
        {
          "sig": "Tick(DateTime now):bool",
          "desc": "Sets CurrentTime to now and runs a tick cycle: if now >= NextTickTime and no stop intention, InvokeTick is called; if stop was requested, LogicStopped is invoked and the LogicStopped event raised. Returns true if a logic is still attached and running, false otherwise. An exception from InvokeTick is reported to ErrorStream, the logic is stopped, and false is returned.",
          "preconditions": [
            "none"
          ],
          "postconditions": [
            "on false: no logic attached (stopped or failed) or stop requested was processed"
          ],
          "example": "while (driver.Tick(DateTime.UtcNow)) { await Task.Delay(1); }"
        },
        {
          "sig": "protected DoTick():bool",
          "desc": "The shared tick-cycle core used by Tick(now) and StopNow. Do not call concurrently with Tick."
        },
        {
          "sig": "StopNow()",
          "desc": "Sets the stop intention and immediately runs DoTick so the current logic's LogicStopped fires without waiting for the next Tick call.",
          "threadSafety": "safe to call from another thread",
          "example": "driver.StopNow();"
        }
      ],
      "events": [
        {
          "sig": "event Action<ILogic<TLogicDriverCtl>>? LogicStopped",
          "desc": "Raised when a logic ends (stop request, failed start, or tick exception). Handlers are protected by try/catch."
        },
        {
          "sig": "event Action<Exception>? ErrorStream",
          "desc": "Raised for exceptions from logic callbacks. Handlers are protected by try/catch."
        }
      ],
      "remarks": "explicit ILogicDriverCtl.Stop() sets the stop intention (processed at the next Tick); Finish() and WaitForFinish() throw NotSupportedException."
    },
    {
      "name": "PeriodicLikeMultiLogicDriver<TManualLogicDriver, TLogicDriverCtl>",
      "kind": "class",
      "category": "driver-multi",
      "base": "object",
      "implements": [
        "ILogicDriver<TLogicDriverCtl>",
        "ISemiPeriodicLogic"
      ],
      "genericConstraints": "where TLogicDriverCtl : class, ILogicDriverCtl; where TManualLogicDriver : PeriodicLikeLogicManualDriver<TLogicDriverCtl>",
      "desc": "Abstract multi-logic scheduler that itself runs as an ISemiPeriodicLogic driven by an outer driver. Manages N child manual drivers through a priority queue keyed by NextTickTime; each child runs one logic. Start returns DriverIsNotActive until this driver has been started as an ISemiPeriodicLogic.",
      "threadSafety": "Start is safe from concurrent threads (concurrent valve + queue); LogicTick runs on the outer driver's single thread",
      "limitations": "Not self-driving — must be started (as ISemiPeriodicLogic) by an outer driver; use the ThreadBased*MultiLogicDriver wrappers for automatic scheduling.",
      "constructors": [
        {
          "sig": "protected ctor()",
          "desc": "Protected base constructor; initializes the valve (with StopNow on close) and the priority queue.",
          "example": "// only called by subclasses"
        }
      ],
      "methods": [
        {
          "sig": "protected abstract ConstructManualDriver():TManualLogicDriver",
          "desc": "Subclasses construct a fresh child manual driver per started logic (e.g. with the fixed period).",
          "example": "protected override ManualPeriodicLogicDriver ConstructManualDriver() => new(_period);"
        },
        {
          "sig": "Start(ILogic<TLogicDriverCtl> logic):LogicStartResult",
          "desc": "If the multi-driver is not Running returns DriverIsNotActive. Otherwise constructs a child manual driver, starts logic on it, and schedules the child. On child start failure the child is discarded and its error result returned.",
          "preconditions": [
            "logic is not null",
            "this driver was started as ISemiPeriodicLogic (or is wrapped by a thread-based driver)"
          ],
          "postconditions": [
            "on Success: exactly one child driver is queued for the logic"
          ],
          "example": "var res = multiDriver.Start(logic);"
        },
        {
          "sig": "LogicStarted(ISemiPeriodicLogicDriverCtl driver):bool",
          "desc": "Explicit ISemiPeriodicLogic impl: sets state to Running and returns true. Called by the outer driver.",
          "example": "// invoked by the outer thread-based driver"
        },
        {
          "sig": "LogicTick(ISemiPeriodicLogicDriverCtl driver):TimeSpan",
          "desc": "Explicit ISemiPeriodicLogic impl: drains newly added children, ticks every child whose NextTickTime <= now, re-schedules survivors, honors Finish (stops the outer driver) and returns the time until the next due child (or a 50ms empty frame when idle). O(log n) per child re-insertion.",
          "example": "// invoked by the outer driver; not called directly"
        },
        {
          "sig": "LogicStopped()",
          "desc": "Explicit ILogic impl: closes the valve, stops all remaining child drivers, and completes the Finish/WaitForFinish task.",
          "example": "// invoked by the outer driver on shutdown"
        },
        {
          "sig": "Finish():Task",
          "desc": "Explicit ILogicDriver impl: sets the finish intention (processed on the next LogicTick) and returns the shared wait task.",
          "example": "await multiDriver.Finish();"
        },
        {
          "sig": "WaitForFinish():Task",
          "desc": "Explicit ILogicDriver impl: returns the shared task that completes when LogicStopped runs.",
          "example": "await multiDriver.WaitForFinish();"
        }
      ],
      "events": [
        {
          "sig": "event Action<ILogic<TLogicDriverCtl>>? LogicStopped",
          "desc": "Forwarded from child manual drivers whenever any managed logic stops."
        },
        {
          "sig": "event Action<Exception>? ErrorStream",
          "desc": "Forwarded from child manual drivers for logic callback exceptions."
        }
      ]
    },
    {
      "name": "ManualPeriodicLogicDriver",
      "kind": "class",
      "category": "driver-manual",
      "base": "PeriodicLikeLogicManualDriver<IPeriodicLogicDriverCtl>",
      "implements": [
        "IPeriodicLogicDriverCtl"
      ],
      "desc": "Manual fixed-period driver. Starts at (DateTime.MinValue + period); each tick advances NextTickTime by the fixed period. Tick when CurrentTime >= NextTickTime.",
      "limitations": "Manual — call Tick(DateTime) externally; use ThreadBasedPeriodicMultiLogicDriver for automation.",
      "constructors": [
        {
          "sig": "ctor(TimeSpan period)",
          "desc": "Creates a manual periodic driver with the given fixed period. Does not validate the period here (PeriodicMultiLogicDriver validates); throws ArgumentOutOfRangeException if period is negative (DateTime.MinValue + period overflows), and a zero period fires a tick on every Tick call.",
          "example": "var driver = new ManualPeriodicLogicDriver(TimeSpan.FromMilliseconds(10));"
        }
      ],
      "methods": [
        {
          "sig": "protected override InvokeStart(ILogic<IPeriodicLogicDriverCtl> logic):bool",
          "desc": "Calls logic.LogicStarted(this) and returns the result."
        },
        {
          "sig": "protected override InvokeTick(ILogic<IPeriodicLogicDriverCtl> logic)",
          "desc": "Casts logic to IPeriodicLogic, calls LogicTick(this), then advances NextTickTime by the fixed period."
        }
      ],
      "properties": [
        {
          "sig": "explicit Period:TimeSpan { get; }",
          "desc": "The fixed period given in the constructor; exposed via IPeriodicLogicDriverCtl.Period."
        }
      ]
    },
    {
      "name": "PeriodicMultiLogicDriver",
      "kind": "class",
      "category": "driver-multi",
      "base": "PeriodicLikeMultiLogicDriver<ManualPeriodicLogicDriver, IPeriodicLogicDriverCtl>",
      "desc": "Fixed-period multi-logic scheduler. Each started logic gets its own ManualPeriodicLogicDriver with the shared period. Must be driven as an ISemiPeriodicLogic by an outer driver.",
      "limitations": "Not self-driving; wrap with ThreadBasedPeriodicMultiLogicDriver unless you drive it yourself.",
      "constructors": [
        {
          "sig": "ctor(TimeSpan period)",
          "desc": "Creates the scheduler; throws ArgumentOutOfRangeException if period <= TimeSpan.Zero.",
          "preconditions": [
            "period > TimeSpan.Zero"
          ],
          "postconditions": [
            "a fixed-period multi scheduler is configured"
          ],
          "example": "var multi = new PeriodicMultiLogicDriver(TimeSpan.FromMilliseconds(10));"
        }
      ],
      "methods": [
        {
          "sig": "protected override ConstructManualDriver():ManualPeriodicLogicDriver",
          "desc": "Returns a new ManualPeriodicLogicDriver with the shared period."
        }
      ]
    },
    {
      "name": "ThreadBasedPeriodicMultiLogicDriver",
      "kind": "class",
      "category": "driver-thread",
      "base": "object",
      "implements": [
        "ILogicDriver<IPeriodicLogicDriverCtl>"
      ],
      "desc": "Self-driving fixed-period multi-logic driver: composes PeriodicMultiLogicDriver with a ThreadBasedSemiPeriodicLogicDriver and starts them in the constructor, so Start works immediately. One background thread serves all logic instances. The driver ends permanently when finished.",
      "threadSafety": "Start and Finish are thread-safe; logic callbacks run on the single background thread",
      "limitations": "Once finished it cannot be restarted; create a new instance.",
      "constructors": [
        {
          "sig": "ctor(IDateTimeProvider timeProvider, TimeSpan period)",
          "desc": "Creates and immediately starts the scheduler thread. period must be > TimeSpan.Zero (validated by PeriodicMultiLogicDriver).",
          "preconditions": [
            "timeProvider is not null",
            "period > TimeSpan.Zero"
          ],
          "postconditions": [
            "the driver is already running and accepting logic"
          ],
          "example": "var driver = new ThreadBasedPeriodicMultiLogicDriver(UtcNowDateTimeProvider.Instance, TimeSpan.FromMilliseconds(10));"
        }
      ],
      "methods": [
        {
          "sig": "Start(ILogic<IPeriodicLogicDriverCtl> logic):LogicStartResult",
          "desc": "Delegates to the internal PeriodicMultiLogicDriver. Returns DriverIsNotActive only if the driver has already finished.",
          "example": "var res = driver.Start(new MyPeriodicLogic());"
        },
        {
          "sig": "Finish():Task",
          "desc": "Explicit ILogicDriver impl: stops the internal thread driver (which stops the multi scheduler) and returns a task that completes when it has finished.",
          "example": "await driver.Finish();"
        },
        {
          "sig": "WaitForFinish():Task",
          "desc": "Explicit ILogicDriver impl: returns the task completing when the driver finishes.",
          "example": "await driver.WaitForFinish();"
        }
      ],
      "events": [
        {
          "sig": "event Action<ILogic<IPeriodicLogicDriverCtl>>? LogicStopped",
          "desc": "Forwarded from the internal multi scheduler whenever a managed logic stops."
        },
        {
          "sig": "event Action<Exception>? ErrorStream",
          "desc": "Raised for errors from the internal scheduler or its thread driver."
        }
      ]
    },
    {
      "name": "ThreadBasedNonPeriodicLogicMultiDriver",
      "kind": "class",
      "category": "driver-thread",
      "base": "object",
      "implements": [
        "ILogicDriver<INonPeriodicLogicDriverCtl>"
      ],
      "desc": "Self-driving on-demand multi-logic driver with its own background thread. Logics run only when they request invocations (RequestInvocation). Start requires the logic to implement INonPeriodicLogic, otherwise FailedToStart. The driver ends permanently when finished.",
      "threadSafety": "Start and RequestInvocation are safe from any thread; ticks run on the single background thread and never overlap",
      "limitations": "Once finished it cannot be restarted; create a new instance.",
      "constructors": [
        {
          "sig": "ctor(IDateTimeProvider timeProvider)",
          "desc": "Creates and immediately starts the background thread. CurrentTime for logic comes from timeProvider.Now.",
          "preconditions": [
            "timeProvider is not null"
          ],
          "postconditions": [
            "the driver is running and accepting logic"
          ],
          "example": "var driver = new ThreadBasedNonPeriodicLogicMultiDriver(UtcNowDateTimeProvider.Instance);"
        }
      ],
      "methods": [
        {
          "sig": "Start(ILogic<INonPeriodicLogicDriverCtl> logic):LogicStartResult",
          "desc": "Explicit ILogicDriver impl. Returns DriverIsNotActive if the driver is destroyed; FailedToStart if logic does not implement INonPeriodicLogic or its LogicStarted returned false/threw; otherwise Success (the logic's LogicStopped and the LogicStopped event fire if startup fails).",
          "preconditions": [
            "logic is not null"
          ],
          "postconditions": [
            "on Success: the logic is registered and will be ticked when it requests invocations"
          ],
          "example": "var res = driver.Start(new MyNonPeriodicLogic());"
        },
        {
          "sig": "Finish():Task",
          "desc": "Explicit ILogicDriver impl: sets the destroy intention, wakes the thread, stops all managed logics, and returns a task completing when the thread exits.",
          "example": "await driver.Finish();"
        },
        {
          "sig": "WaitForFinish():Task",
          "desc": "Explicit ILogicDriver impl: returns the task completing when the driver finishes.",
          "example": "await driver.WaitForFinish();"
        }
      ],
      "events": [
        {
          "sig": "event Action<ILogic<INonPeriodicLogicDriverCtl>>? LogicStopped",
          "desc": "Raised when a managed logic stops (requested stop, finish, or tick exception)."
        },
        {
          "sig": "event Action<Exception>? ErrorStream",
          "desc": "Raised for exceptions thrown by logic callbacks."
        }
      ]
    },
    {
      "name": "ManualSemiPeriodicLogicDriver",
      "kind": "class",
      "category": "driver-manual",
      "base": "PeriodicLikeLogicManualDriver<ISemiPeriodicLogicDriverCtl>",
      "implements": [
        "ISemiPeriodicLogicDriverCtl"
      ],
      "desc": "Manual self-delaying driver. Each tick asks the logic (ISemiPeriodicLogic) for the next delay and schedules NextTickTime accordingly. Internal clock starts at DateTime.MinValue.AddSeconds(1).",
      "limitations": "Manual — call Tick(DateTime) externally; use ThreadBasedSemiPeriodicLogicDriver/MultiLogic for automation.",
      "constructors": [
        {
          "sig": "ctor()",
          "desc": "Creates the driver; prev/current/next tick times are initialized to DateTime.MinValue.AddSeconds(1).",
          "example": "var driver = new ManualSemiPeriodicLogicDriver();"
        }
      ],
      "methods": [
        {
          "sig": "protected override InvokeStart(ILogic<ISemiPeriodicLogicDriverCtl> logic):bool",
          "desc": "Calls logic.LogicStarted(this) and returns the result."
        },
        {
          "sig": "protected override InvokeTick(ILogic<ISemiPeriodicLogicDriverCtl> logic)",
          "desc": "Casts logic to ISemiPeriodicLogic, calls LogicTick(this); if the returned delay is negative, throws InvalidOperationException (reported via ErrorStream and the logic is stopped). Otherwise sets NextTickTime = prev tick time + delay."
        }
      ]
    },
    {
      "name": "SemiPeriodicMultiLogicDriver",
      "kind": "class",
      "category": "driver-multi",
      "base": "PeriodicLikeMultiLogicDriver<ManualSemiPeriodicLogicDriver, ISemiPeriodicLogicDriverCtl>",
      "desc": "Multi-logic scheduler where each logic instance controls its own delay (semi-periodic). Each started logic gets a ManualSemiPeriodicLogicDriver. Must be driven as an ISemiPeriodicLogic by an outer driver.",
      "limitations": "Not self-driving; wrap with ThreadBasedSemiPeriodicMultiLogicDriver unless you drive it yourself.",
      "constructors": [
        {
          "sig": "ctor()",
          "desc": "Creates the scheduler. Start returns DriverIsNotActive until this driver is itself started as an ISemiPeriodicLogic by an outer driver."
        }
      ],
      "methods": [
        {
          "sig": "protected override ConstructManualDriver():ManualSemiPeriodicLogicDriver",
          "desc": "Returns a new ManualSemiPeriodicLogicDriver."
        }
      ]
    },
    {
      "name": "ThreadBasedSemiPeriodicLogicDriver",
      "kind": "class",
      "category": "driver-thread",
      "base": "object",
      "implements": [
        "ILogicDriver<ISemiPeriodicLogicDriverCtl>"
      ],
      "desc": "Single-logic, self-driving semi-periodic driver. Background thread wakes on an AutoResetEvent, ticks the internal ManualSemiPeriodicLogicDriver, then sleeps until the next due tick. finishOnComplete=true stops the driver permanently when the logic completes.",
      "threadSafety": "Start/Finish guarded by a lock; logic callbacks run on the single background thread",
      "limitations": "Holds exactly one logic. If finishOnComplete, the driver ends (and cannot be reused) once the logic stops.",
      "constructors": [
        {
          "sig": "ctor(IDateTimeProvider timeProvider, bool finishOnComplete)",
          "desc": "Creates and immediately starts the background thread. When finishOnComplete is true the driver stops itself once the logic stops; when false it keeps the thread alive for future Start calls.",
          "preconditions": [
            "timeProvider is not null"
          ],
          "postconditions": [
            "the driver thread is running"
          ],
          "example": "var driver = new ThreadBasedSemiPeriodicLogicDriver(UtcNowDateTimeProvider.Instance, true);"
        }
      ],
      "methods": [
        {
          "sig": "Start(ILogic<ISemiPeriodicLogicDriverCtl> logic):LogicStartResult",
          "desc": "Starts the single logic on the internal manual driver and wakes the thread. Returns DriverIsNotActive if the driver has already finished; on failure, if finishOnComplete is true, Finish() is requested. LogicStartResult.FailedToStart when LogicStarted failed.",
          "preconditions": [
            "logic is not null"
          ],
          "postconditions": [
            "on Success: the logic runs on the background thread"
          ],
          "example": "var res = driver.Start(new MySemiPeriodicLogic());"
        },
        {
          "sig": "Finish():Task",
          "desc": "Requests shutdown (disposes the reset event, stops the thread) and returns a task completing when the thread exits and the wait task is set.",
          "threadSafety": "thread-safe",
          "example": "await driver.Finish();"
        },
        {
          "sig": "WaitForFinish():Task",
          "desc": "Returns the shared task that completes when the driver finishes.",
          "threadSafety": "thread-safe",
          "example": "await driver.WaitForFinish();"
        }
      ],
      "events": [
        {
          "sig": "event Action<ILogic<ISemiPeriodicLogicDriverCtl>>? LogicStopped",
          "desc": "Raised when the managed logic stops (forwarded from the internal manual driver)."
        },
        {
          "sig": "event Action<Exception>? ErrorStream",
          "desc": "Raised for errors from the internal manual driver."
        }
      ]
    },
    {
      "name": "ThreadBasedSemiPeriodicMultiLogicDriver",
      "kind": "class",
      "category": "driver-thread",
      "base": "object",
      "implements": [
        "ILogicDriver<ISemiPeriodicLogicDriverCtl>"
      ],
      "desc": "Self-driving semi-periodic multi-logic driver: composes SemiPeriodicMultiLogicDriver with a ThreadBasedSemiPeriodicLogicDriver and starts them in the constructor. Each managed logic chooses its own delay. The driver ends permanently when finished.",
      "threadSafety": "Start and Finish are thread-safe; logic callbacks run on the single background thread",
      "limitations": "Once finished it cannot be restarted; create a new instance.",
      "constructors": [
        {
          "sig": "ctor(IDateTimeProvider timeProvider)",
          "desc": "Creates and immediately starts the scheduler thread.",
          "preconditions": [
            "timeProvider is not null"
          ],
          "postconditions": [
            "the driver is already running and accepting logic"
          ],
          "example": "var driver = new ThreadBasedSemiPeriodicMultiLogicDriver(UtcNowDateTimeProvider.Instance);"
        }
      ],
      "methods": [
        {
          "sig": "Start(ILogic<ISemiPeriodicLogicDriverCtl> logic):LogicStartResult",
          "desc": "Delegates to the internal SemiPeriodicMultiLogicDriver. Returns DriverIsNotActive only if the driver has already finished.",
          "example": "var res = driver.Start(new MySemiPeriodicLogic());"
        },
        {
          "sig": "Finish():Task",
          "desc": "Explicit ILogicDriver impl: stops the internal thread driver and returns a task completing when it finishes.",
          "example": "await driver.Finish();"
        },
        {
          "sig": "WaitForFinish():Task",
          "desc": "Explicit ILogicDriver impl: returns the task completing when the driver finishes.",
          "example": "await driver.WaitForFinish();"
        }
      ],
      "events": [
        {
          "sig": "event Action<ILogic<ISemiPeriodicLogicDriverCtl>>? LogicStopped",
          "desc": "Forwarded from the internal multi scheduler whenever a managed logic stops."
        },
        {
          "sig": "event Action<Exception>? ErrorStream",
          "desc": "Raised for errors from the internal scheduler or its thread driver."
        }
      ]
    }
  ],
  "extensions": [
    {
      "for": "IPeriodicLogic",
      "members": [
        {
          "sig": "Test(this IPeriodicLogic core, Action<string> onFail):IPeriodicLogic",
          "desc": "In DEBUG builds wraps core in an invariant checker that enforces the Constructed -> Started -> Stopped state machine (LogicStarted before LogicTick before LogicStopped, no double-stops); violations are reported via onFail and the wrapper still forwards to core. In Release builds returns core unchanged (no-op). Use in tests to catch driver/logic protocol bugs.",
          "preconditions": [
            "core is not null",
            "onFail is not null"
          ],
          "postconditions": [
            "DEBUG: returns an IPeriodicLogic wrapping core",
            "Release: returns core as-is"
          ],
          "example": "var wrapped = myLogic.Test(msg => Console.WriteLine(msg));",
          "remarks": "The wrapper serializes callback calls with a critical section, so it also guards against re-entrant misuse in DEBUG."
        }
      ]
    }
  ],
  "gotchas": [
    "Manual drivers (PeriodicLikeLogicManualDriver subclasses: ManualPeriodicLogicDriver, ManualSemiPeriodicLogicDriver) throw NotSupportedException from Finish() and WaitForFinish() — you end them by calling Tick until it returns false or by calling StopNow().",
    "After a graceful stop (Stop(), StopNow(), or the logic calling driver.Stop()), the manual driver still holds its logic reference, so IsRunning stays true and a subsequent Tick can re-invoke LogicTick on the stopped logic; once Tick returns false, stop calling Tick and use a fresh driver for the next run — as a result, a ThreadBasedSemiPeriodicLogicDriver(finishOnComplete:false) cannot start a second logic (Start returns CapacityExceeded).",
    "PeriodicLikeMultiLogicDriver (and PeriodicMultiLogicDriver / SemiPeriodicMultiLogicDriver used directly) return DriverIsNotActive from Start until the driver is itself started as an ISemiPeriodicLogic by an outer driver; the ThreadBased*MultiLogicDriver wrappers do this automatically in their constructor.",
    "SingleJobLogicDriver's gate is never reset: after the wrapped logic stops, every further Start returns CapacityExceeded — create a fresh wrapper for each run.",
    "The LogicStopped event fires on every termination, including failed starts (LogicStarted returning false) and exceptions thrown from ticks/stops — do not assume it only means graceful completion.",
    "Exceptions thrown from LogicTick / LogicStarted / LogicStopped never propagate to the caller of Start or Tick: the driver catches them, reports them via ErrorStream, and stops the logic (invoking LogicStopped).",
    "ThreadBasedNonPeriodicLogicMultiDriver.Start returns FailedToStart if the logic does not implement INonPeriodicLogic — passing a plain ILogic fails silently this way.",
    "RequestInvocation is asynchronous: the tick is queued and executed on the driver's background thread, never inline on the caller's thread; repeated requests while a tick is pending are coalesced into one tick.",
    "ISemiPeriodicLogic.LogicTick must not return a negative TimeSpan — ManualSemiPeriodicLogicDriver throws InvalidOperationException and then stops the logic.",
    "PeriodicMultiLogicDriver validates period > TimeSpan.Zero and throws ArgumentOutOfRangeException; ManualPeriodicLogicDriver does not validate — a zero period fires a tick on every Tick call and a negative period throws ArgumentOutOfRangeException at construction (DateTime.MinValue + period overflows).",
    "Thread-based drivers use the injected IDateTimeProvider as their only time source — mixing NowDateTimeProvider (local) and UtcNowDateTimeProvider (UTC) between the driver and logic expectations will skew scheduling; be consistent.",
    "All ThreadBased* drivers start their background thread in the constructor and, once finished (Finish or finishOnComplete), are permanently done — do not attempt to reuse them.",
    "ThreadSafeDateTime.Now returns the last Time written; with a frozen clock, periodic/semi-periodic thread-based drivers tick once and then stall because they only re-read Now when their real-time sleep elapses — advancing the clock does not wake the sleeping driver thread."
  ],
  "commonMistakes": [
    "Calling Finish() or awaiting WaitForFinish() on a manual driver (throws NotSupportedException).",
    "Reusing a SingleJobLogicDriver for a second logic run and being surprised by CapacityExceeded.",
    "Using PeriodicMultiLogicDriver or SemiPeriodicMultiLogicDriver directly without driving them as ISemiPeriodicLogic, getting DriverIsNotActive from Start.",
    "Forgetting to inject a fake clock (ThreadSafeDateTime) into thread-based drivers, making tests depend on wall-clock timing.",
    "Subscribing only to LogicStopped and never to ErrorStream, then wondering why failures are invisible — logic exceptions only surface through ErrorStream.",
    "Returning a negative delay from ISemiPeriodicLogic.LogicTick and triggering InvalidOperationException.",
    "Calling RequestInvocation from LogicStarted and expecting the first tick to be synchronous/inline.",
    "Implementing ILogic<T> instead of the concrete IPeriodicLogic / ISemiPeriodicLogic / INonPeriodicLogic subtype and receiving FailedToStart from ThreadBasedNonPeriodicLogicMultiDriver.",
    "Starting two different logics on a single-logic driver (ManualPeriodicLogicDriver, ThreadBasedSemiPeriodicLogicDriver) and treating the CapacityExceeded result as an error bug."
  ],
  "patterns": [
    {
      "goal": "Run a fixed-period logic until it stops itself, awaiting its end",
      "code": "var driver = new ThreadBasedPeriodicMultiLogicDriver(UtcNowDateTimeProvider.Instance, TimeSpan.FromMilliseconds(10)); var res = driver.Start(new MyPeriodicLogic()); if (res == LogicStartResult.Success) { await driver.WaitForFinish(); }"
    },
    {
      "goal": "Drive a manual driver deterministically from a test with an external clock",
      "code": "var driver = new ManualPeriodicLogicDriver(TimeSpan.FromMilliseconds(10)); driver.Start(new MyPeriodicLogic()); DateTime now = DateTime.UtcNow; while (driver.Tick(now)) { now = now.AddMilliseconds(10); }"
    },
    {
      "goal": "Graceful degradation: handle every possible Start result explicitly",
      "code": "var res = driver.Start(logic); if (res == LogicStartResult.Success) { await driver.WaitForFinish(); } else if (res == LogicStartResult.CapacityExceeded) { ReuseAnotherDriver(); } else if (res == LogicStartResult.DriverIsNotActive) { StartOuterDriverFirst(); } else { LogStartFailure(logic, res); }"
    },
    {
      "goal": "A self-stopping periodic logic that ends its own execution",
      "code": "void IPeriodicLogic.LogicTick(IPeriodicLogicDriverCtl d) { DoWork(); if (IsComplete) { d.Stop(); } }"
    },
    {
      "goal": "Semi-periodic logic that chooses its next delay and guards against negative values",
      "code": "TimeSpan ISemiPeriodicLogic.LogicTick(ISemiPeriodicLogicDriverCtl d) { var delay = ComputeNextDelay(); return delay < TimeSpan.Zero ? TimeSpan.Zero : delay; }"
    },
    {
      "goal": "On-demand logic that re-requests its next invocation quantum",
      "code": "void INonPeriodicLogic.LogicTick(INonPeriodicLogicDriverCtl d) { ConsumeNextItem(); if (HasMore) { d.RequestInvocation(); } else { d.Stop(); } }"
    },
    {
      "goal": "Graceful handling of a tick that reports the driver stopped (returns false)",
      "code": "bool running = true; while (running) { running = driver.Tick(DateTime.UtcNow); if (!running) { Cleanup(); } }"
    },
    {
      "goal": "Single-job wrapper with error streaming for a thread-based multi driver",
      "code": "var driver = new SingleJobLogicDriver<IPeriodicLogicDriverCtl>(new ThreadBasedPeriodicMultiLogicDriver(UtcNowDateTimeProvider.Instance, TimeSpan.FromMilliseconds(10))); driver.ErrorStream += ex => Console.WriteLine(ex); var res = driver.Start(logic); await driver.WaitForFinish();"
    },
    {
      "goal": "Deterministic scheduling by advancing a fake clock injected into a thread-based driver",
      "code": "var clock = new ThreadSafeDateTime(DateTime.UtcNow); var driver = new ThreadBasedPeriodicMultiLogicDriver(clock, TimeSpan.FromMilliseconds(10)); driver.Start(logic); clock.Time = clock.Time.AddSeconds(1); await driver.WaitForFinish();"
    },
    {
      "goal": "Enable invariant checking of the lifecycle state machine in debug tests",
      "code": "var wrapped = new MyPeriodicLogic().Test(msg => Assert.Fail(msg)); driver.Start(wrapped);"
    }
  ],
  "relationships": [
    {
      "from": "SingleJobLogicDriver<T>",
      "to": "ILogicDriver<T>",
      "rel": "takes the core driver in its constructor and wraps it"
    },
    {
      "from": "ManualPeriodicLogicDriver",
      "to": "PeriodicLikeLogicManualDriver<IPeriodicLogicDriverCtl>",
      "rel": "inherits from"
    },
    {
      "from": "ManualSemiPeriodicLogicDriver",
      "to": "PeriodicLikeLogicManualDriver<ISemiPeriodicLogicDriverCtl>",
      "rel": "inherits from"
    },
    {
      "from": "PeriodicMultiLogicDriver",
      "to": "ManualPeriodicLogicDriver",
      "rel": "creates instances via ConstructManualDriver"
    },
    {
      "from": "SemiPeriodicMultiLogicDriver",
      "to": "ManualSemiPeriodicLogicDriver",
      "rel": "creates instances via ConstructManualDriver"
    },
    {
      "from": "ThreadBasedPeriodicMultiLogicDriver",
      "to": "PeriodicMultiLogicDriver",
      "rel": "composes it as the scheduler"
    },
    {
      "from": "ThreadBasedSemiPeriodicMultiLogicDriver",
      "to": "SemiPeriodicMultiLogicDriver",
      "rel": "composes it as the scheduler"
    },
    {
      "from": "ThreadBasedPeriodicMultiLogicDriver",
      "to": "ThreadBasedSemiPeriodicLogicDriver",
      "rel": "composes it as the outer driver"
    },
    {
      "from": "ThreadBasedSemiPeriodicMultiLogicDriver",
      "to": "ThreadBasedSemiPeriodicLogicDriver",
      "rel": "composes it as the outer driver"
    },
    {
      "from": "ThreadBasedSemiPeriodicLogicDriver",
      "to": "ManualSemiPeriodicLogicDriver",
      "rel": "wraps and ticks it on a background thread"
    },
    {
      "from": "ThreadBasedNonPeriodicLogicMultiDriver",
      "to": "INonPeriodicLogicDriverCtl",
      "rel": "creates a private implementation per started logic"
    },
    {
      "from": "PeriodicLikeMultiLogicDriver",
      "to": "ISemiPeriodicLogic",
      "rel": "implements it; must be driven by an outer driver"
    },
    {
      "from": "ThreadBasedPeriodicMultiLogicDriver",
      "to": "UtcNowDateTimeProvider",
      "rel": "accepts it as the IDateTimeProvider clock"
    },
    {
      "from": "ThreadSafeDateTime",
      "to": "IDateTimeProvider",
      "rel": "implements it as a mutable fake clock"
    },
    {
      "from": "IPeriodicLogic",
      "to": "ILogic<IPeriodicLogicDriverCtl>",
      "rel": "extends"
    },
    {
      "from": "ISemiPeriodicLogic",
      "to": "ILogic<ISemiPeriodicLogicDriverCtl>",
      "rel": "extends"
    },
    {
      "from": "INonPeriodicLogic",
      "to": "ILogic<INonPeriodicLogicDriverCtl>",
      "rel": "extends"
    },
    {
      "from": "IPeriodicLogicDriverCtl",
      "to": "ILogicDriverCtl",
      "rel": "extends"
    },
    {
      "from": "ISemiPeriodicLogicDriverCtl",
      "to": "ILogicDriverCtl",
      "rel": "extends"
    },
    {
      "from": "INonPeriodicLogicDriverCtl",
      "to": "ILogicDriverCtl",
      "rel": "extends"
    },
    {
      "from": "IUtcDateTimeProvider",
      "to": "IDateTimeProvider",
      "rel": "extends"
    },
    {
      "from": "UtcNowDateTimeProvider",
      "to": "IUtcDateTimeProvider",
      "rel": "implements"
    },
    {
      "from": "NowDateTimeProvider",
      "to": "IDateTimeProvider",
      "rel": "implements"
    }
  ]
}