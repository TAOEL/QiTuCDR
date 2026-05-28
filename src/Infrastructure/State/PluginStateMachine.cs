using System;
using QiTuCDR.Shared;

namespace QiTuCDR.Infrastructure.State
{
    public sealed class PluginStateMachine
    {
        private readonly object _sync = new object();

        public PluginState Current { get; private set; } = PluginState.Starting;

        public event EventHandler<PluginState>? StateChanged;

        public bool CanAcceptBusinessRequest => Current == PluginState.Ready;

        public void TransitionTo(PluginState next)
        {
            lock (_sync)
            {
                if (!IsAllowed(Current, next))
                {
                    throw new InvalidOperationException($"Illegal plugin state transition: {Current} -> {next}");
                }

                if (Current == next)
                {
                    return;
                }

                Current = next;
            }

            StateChanged?.Invoke(this, next);
        }

        public bool TryEnterBusy()
        {
            lock (_sync)
            {
                if (Current != PluginState.Ready)
                {
                    return false;
                }

                Current = PluginState.Busy;
            }

            StateChanged?.Invoke(this, PluginState.Busy);
            return true;
        }

        public void CompleteBusy()
        {
            lock (_sync)
            {
                if (Current != PluginState.Busy)
                {
                    return;
                }

                Current = PluginState.Ready;
            }

            StateChanged?.Invoke(this, PluginState.Ready);
        }

        private static bool IsAllowed(PluginState current, PluginState next)
        {
            if (current == next)
            {
                return true;
            }

            if (next == PluginState.Faulted)
            {
                return current != PluginState.Disposed;
            }

            switch (current)
            {
                case PluginState.Starting:
                    return next == PluginState.Preheating || next == PluginState.Disposing;
                case PluginState.Preheating:
                    return next == PluginState.Ready || next == PluginState.Recovering || next == PluginState.Disposing;
                case PluginState.Ready:
                    return next == PluginState.Busy || next == PluginState.Disposing || next == PluginState.Recovering;
                case PluginState.Busy:
                    return next == PluginState.Ready || next == PluginState.Disposing;
                case PluginState.Faulted:
                    return next == PluginState.Recovering || next == PluginState.Disposing;
                case PluginState.Recovering:
                    return next == PluginState.Ready || next == PluginState.Disposing || next == PluginState.Faulted;
                case PluginState.Disposing:
                    return next == PluginState.Disposed;
                case PluginState.Disposed:
                    return false;
                default:
                    return false;
            }
        }
    }
}
