namespace Operarius.Tests
{
    public abstract class PeriodicLikeTester<TStartTickStop, TDriverCtl>
        where TDriverCtl : class, ILogicDriverCtl
        where TStartTickStop : PeriodicLikeTestLogic<TDriverCtl>, new()
    {
        protected abstract ILogicDriver<TDriverCtl> GetDriver();
        
        [Test]
        public Task RunOk()
        {
            return DoTest(() => new TStartTickStop());
        }
        
        [Test]
        public Task RunErrorOnStart()
        {
            return DoTest(() => new TStartTickStop() { ErrorOnStart = true });
        }
        
        [Test]
        public Task RunExceptionOnStart()
        {
            return DoTest(() => new TStartTickStop() { CrashOnStart = true });
        }
        
        [Test]
        public Task RunExceptionOnTick()
        {
            return DoTest(() => new TStartTickStop() { CrashOnTick = true });
        }
        
        [Test]
        public Task RunExceptionOnStop()
        {
            return DoTest(() => new TStartTickStop() { CrashOnStop = true});
        }
        
        [Test]
        public Task RunExceptionOnStartAndStop()
        {
            return DoTest(() => new TStartTickStop() { CrashOnStart = true, CrashOnStop = true});
        }
        
        [Test]
        public Task RunExceptionOnTickAndStop()
        {
            return DoTest(() => new TStartTickStop() { CrashOnTick = true, CrashOnStop = true});
        }

        private async Task DoTest(Func<TStartTickStop> logicFactory)
        {
            await DoTest(logicFactory(), GetDriver());
        }

        private async Task DoTest(PeriodicLikeTestLogic<TDriverCtl> logic, ILogicDriver<TDriverCtl> driver)
        {
            List<Exception> exceptions = new List<Exception>();
            
            driver.ErrorStream += ex => exceptions.Add(ex);
            
            TaskCompletionSource tcs = new TaskCompletionSource();
            driver.LogicStopped += l => tcs.SetResult();

            var startResult = driver.Start(logic);

            if (logic.ErrorOnStart || logic.CrashOnStart)
            {
                if (logic.ErrorOnStart)
                {
                    Assert.That(startResult, Is.EqualTo(LogicStartResult.FailedToStart));
                    Assert.That(exceptions.Count, Is.EqualTo(0));
                    Assert.That(logic.StartInvoked, Is.EqualTo(true));
                    Assert.That(logic.TickInvoked, Is.EqualTo(0));
                    Assert.That(logic.StopInvoked, Is.EqualTo(true));
                }
                else if (logic.CrashOnStart)
                {
                    Assert.That(startResult, Is.EqualTo(LogicStartResult.FailedToStart));
                    Assert.That(exceptions.Count, Is.EqualTo(logic.CrashOnStop ? 2 : 1));
                    Assert.That(logic.StartInvoked, Is.EqualTo(true));
                    Assert.That(logic.TickInvoked, Is.EqualTo(0));
                    Assert.That(logic.StopInvoked, Is.EqualTo(true));
                }
                
                switch (driver)
                {
                    case ManualPeriodicLogicDriver:
                    case ManualSemiPeriodicLogicDriver:
                        break;
                    default:
                        await driver.Finish();
                        break;
                }
                return;
            }

            Assert.That(startResult, Is.EqualTo(LogicStartResult.Success));
            
            switch (driver)
            {
                case ManualPeriodicLogicDriver manualPeriodicDriver:
                    await Drive(manualPeriodicDriver, TimeSpan.FromSeconds(1));
                    break;
                case ManualSemiPeriodicLogicDriver manualSemiPeriodicDriver:
                    await Drive(manualSemiPeriodicDriver, TimeSpan.FromSeconds(1));
                    break;
                default:
                    await tcs.Task;
                    await driver.Finish();
                    break;
            }
            
            Assert.That(logic.StopInvoked, Is.EqualTo(true));
            
            if (logic.CrashOnTick && logic.CrashOnStop)
            {
                Assert.That(exceptions.Count, Is.EqualTo(2));
                Assert.That(logic.TickInvoked, Is.EqualTo(1));
            }
            else if (logic.CrashOnTick)
            {
                Assert.That(exceptions.Count, Is.EqualTo(1));
                Assert.That(logic.TickInvoked, Is.EqualTo(1));
            }
            else if (logic.CrashOnStop)
            {
                Assert.That(exceptions.Count, Is.EqualTo(1));
                Assert.That(logic.TickInvoked, Is.EqualTo(logic.TicksCount));
            }
            else
            {
                Assert.That(exceptions.Count, Is.EqualTo(0));
                Assert.That(logic.TickInvoked, Is.EqualTo(logic.TicksCount));
            }
        }

        private static async Task Drive<T>(PeriodicLikeLogicManualDriver<T> driver, TimeSpan timeOut)
            where T : class, ILogicDriverCtl
        {
            DateTime startTime = DateTime.UtcNow;
            while (true)
            {
                if (DateTime.UtcNow - startTime > timeOut)
                {
                    Assert.Fail("Test timed out");
                    return;
                }

                if (!driver.Tick(DateTime.UtcNow))
                {
                    return;
                }
                await Task.Delay(1);
            }
        }
    }
}