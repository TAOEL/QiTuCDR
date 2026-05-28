using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QiTuCDR.Infrastructure.State;
using QiTuCDR.Shared;

namespace QiTuCDR.Tests
{
    [TestClass]
    public sealed class PluginStateMachineTests
    {
        [TestMethod]
        public void AllowsNormalStartupAndBusyCycle()
        {
            var stateMachine = new PluginStateMachine();

            stateMachine.TransitionTo(PluginState.Preheating);
            stateMachine.TransitionTo(PluginState.Ready);

            Assert.IsTrue(stateMachine.TryEnterBusy());
            Assert.AreEqual(PluginState.Busy, stateMachine.Current);

            stateMachine.CompleteBusy();
            Assert.AreEqual(PluginState.Ready, stateMachine.Current);
        }

        [TestMethod]
        public void RejectsStartingToReadyDirectly()
        {
            var stateMachine = new PluginStateMachine();

            Assert.ThrowsException<InvalidOperationException>(() => stateMachine.TransitionTo(PluginState.Ready));
        }
    }
}
