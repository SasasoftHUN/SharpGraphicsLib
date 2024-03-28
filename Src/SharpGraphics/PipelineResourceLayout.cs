using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SharpGraphics
{
    public abstract class PipelineResourceLayout : IDisposable
    {

        #region Fields

        private bool _isDisposed;

        #endregion

        #region Constructors

        // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~PipelineResourceLayout()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        #endregion

        #region Protected Methods

        [Conditional("DEBUG")]
        protected virtual void AssertLayout(in PipelineResourceProperties resourceProperties)
        {
            ReadOnlySpan<PipelineResourceProperty> properties = resourceProperties.Properties;

            Debug.Assert(properties.Length > 0, "Must have at least one PipelineResourceProperty to create a PipelineResourceLayout");

            for (int i = 0; i < properties.Length; i++)
                for (int j = i + 1; j < properties.Length; j++)
                    Debug.Assert(properties[i].uniqueBinding != properties[j].uniqueBinding, $"PipelineResourceProperties at index {i} and {j} have the same UniqueBinding {properties[i].uniqueBinding}");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                _isDisposed = true;
            }
        }

        #endregion

        #region Public Methods

        public abstract PipelineResource CreateResource();
        public abstract PipelineResource[] CreateResources(uint count);

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
