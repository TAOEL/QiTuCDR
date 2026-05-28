using System;

namespace QiTuCDR.Host.Docking
{
    public sealed class CorelDockerAdapter : ICorelDockerAdapter
    {
        private const string NotImplementedMessage = "CorelDRAW Docker official API adapter is not implemented yet. Keep DockHostMode as Debug until the official CorelDRAW Docker API has been bound and validated.";
        private bool _disposed;

        public bool IsVisible => false;

        public void CreateContainer(object? corelApplication)
        {
            ThrowIfDisposed();
            ThrowOfficialApiNotImplemented(nameof(CreateContainer));
        }

        public void AttachPanel(QiTuDockPanel panel)
        {
            ThrowIfDisposed();
            ThrowOfficialApiNotImplemented(nameof(AttachPanel));
        }

        public void Show()
        {
            ThrowIfDisposed();
            ThrowOfficialApiNotImplemented(nameof(Show));
        }

        public void Hide()
        {
            ThrowIfDisposed();
            ThrowOfficialApiNotImplemented(nameof(Hide));
        }

        public void Release()
        {
            ThrowIfDisposed();
            ThrowOfficialApiNotImplemented(nameof(Release));
        }

        public void Dispose()
        {
            _disposed = true;
        }

        private static void ThrowOfficialApiNotImplemented(string stepName)
        {
            throw new NotSupportedException(NotImplementedMessage + " Missing official API step: " + stepName + ".");
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CorelDockerAdapter));
            }
        }
    }
}
