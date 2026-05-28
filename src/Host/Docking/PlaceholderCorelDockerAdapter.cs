using System;

namespace QiTuCDR.Host.Docking
{
    public sealed class PlaceholderCorelDockerAdapter : ICorelDockerAdapter
    {
        private const string NotImplementedMessage = "CorelDRAW Docker adapter step is not implemented yet. Confirm the official CorelDRAW Docker API and registration mechanism before enabling this host.";
        private bool _disposed;

        public bool IsVisible => false;

        public void CreateContainer(object? corelApplication)
        {
            ThrowIfDisposed();
            ThrowDockerStepNotImplemented(nameof(CreateContainer));
        }

        public void AttachPanel(QiTuDockPanel panel)
        {
            ThrowIfDisposed();
            ThrowDockerStepNotImplemented(nameof(AttachPanel));
        }

        public void Show()
        {
            ThrowIfDisposed();
            ThrowDockerStepNotImplemented(nameof(Show));
        }

        public void Hide()
        {
            ThrowIfDisposed();
            ThrowDockerStepNotImplemented(nameof(Hide));
        }

        public void Release()
        {
            ThrowIfDisposed();
            ThrowDockerStepNotImplemented(nameof(Release));
        }

        public void Dispose()
        {
            _disposed = true;
        }

        private static void ThrowDockerStepNotImplemented(string stepName)
        {
            throw new NotSupportedException(NotImplementedMessage + " Missing adapter step: " + stepName + ".");
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PlaceholderCorelDockerAdapter));
            }
        }
    }
}
