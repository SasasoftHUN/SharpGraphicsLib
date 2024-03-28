using System;
using SharpGraphics.GraphicsViews;

namespace SharpGraphics
{

    //TODO: Use Attributes and Compiler magic to determine OpetatingSystem
    public enum OperatingSystem
    {
        Unknown = 0,
        Windows = 32, UWP = 33,
        Linux = 64,
        Android = 96,
        //Tizen = 128,
        MacOS = 160,
        //iOS = 192,
    }
    public enum DebugLevel
    {
        None, Errors, Important, Everything,
    }

    public readonly struct GraphicsDeviceRequest
    {

        private readonly CommandProcessorGroupRequest[] _commandProcessorGroupRequests;

        public readonly uint deviceIndex;
        public readonly IGraphicsView? graphicsView;
        public readonly PresentCommandProcessorRequest presentCommandProcessor;

        public readonly ReadOnlySpan<CommandProcessorGroupRequest> CommandProcessorGroupRequests => _commandProcessorGroupRequests;

        public GraphicsDeviceRequest(uint deviceIndex, IGraphicsView graphicsView, in PresentCommandProcessorRequest presentCommandProcessor)
        {
            _commandProcessorGroupRequests = new CommandProcessorGroupRequest[] { new CommandProcessorGroupRequest(presentCommandProcessor.groupIndex, 1u) };
            this.deviceIndex = deviceIndex;
            this.graphicsView = graphicsView;
            this.presentCommandProcessor = presentCommandProcessor;
        }
        public GraphicsDeviceRequest(uint deviceIndex, ReadOnlySpan<CommandProcessorGroupRequest> commandProcessorGroupRequests)
        {
            _commandProcessorGroupRequests = commandProcessorGroupRequests.ToArray(); //Copy for safety
            this.deviceIndex = deviceIndex;
            this.graphicsView = null;
            this.presentCommandProcessor = new PresentCommandProcessorRequest();
        }
        public GraphicsDeviceRequest(uint deviceIndex, ReadOnlySpan<CommandProcessorGroupRequest> commandProcessorGroupRequests, IGraphicsView graphicsView, in PresentCommandProcessorRequest presentCommandProcessor)
        {
            _commandProcessorGroupRequests = commandProcessorGroupRequests.ToArray(); //Copy for safety
            this.deviceIndex = deviceIndex;
            this.graphicsView = graphicsView;
            this.presentCommandProcessor = presentCommandProcessor;
        }

    }
    public readonly struct CommandProcessorGroupRequest
    {
        private readonly CommandProcessorRequest[] _commandProcessorRequests;

        public readonly uint groupIndex;

        public ReadOnlySpan<CommandProcessorRequest> CommandProcessorRequests => _commandProcessorRequests;
        public uint Count => _commandProcessorRequests != null ? (uint)_commandProcessorRequests.Length : 0u;

        public CommandProcessorGroupRequest(uint groupIndex)
        {
            this.groupIndex = groupIndex;
            _commandProcessorRequests = new CommandProcessorRequest[] { new CommandProcessorRequest(1f) };
        }
        public CommandProcessorGroupRequest(uint groupIndex, uint count)
        {
            this.groupIndex = groupIndex;
            _commandProcessorRequests = new CommandProcessorRequest[count];
            for (int i = 0; i < _commandProcessorRequests.Length; i++)
                _commandProcessorRequests[i] = new CommandProcessorRequest(1f);
        }
        public CommandProcessorGroupRequest(uint groupIndex, ReadOnlySpan<CommandProcessorRequest> commandProcessorRequests)
        {
            this.groupIndex = groupIndex;
            _commandProcessorRequests = commandProcessorRequests.ToArray(); //Copy for safety
        }
    }
    public readonly struct CommandProcessorRequest
    {
        public readonly float priority;

        public CommandProcessorRequest(float priority)
        {
            this.priority = priority;
        }
    }
    public readonly struct PresentCommandProcessorRequest
    {
        public readonly uint groupIndex;
        public readonly uint commandProcessorIndex;

        public PresentCommandProcessorRequest(uint groupIndex)
        {
            this.groupIndex = groupIndex;
            this.commandProcessorIndex = 0u;
        }
        public PresentCommandProcessorRequest(uint groupIndex, uint commandProcessorIndex)
        {
            this.groupIndex = groupIndex;
            this.commandProcessorIndex = commandProcessorIndex;
        }
    }

    public abstract class GraphicsManagement : IDisposable
    {

        #region Fields

        private bool _isDisposed;

        protected readonly OperatingSystem _operatingSystem;
        protected readonly DebugLevel _debugLevel;

        #endregion

        #region Properties

        public OperatingSystem OperatingSystem => _operatingSystem;
        public DebugLevel DebugLevel => _debugLevel;

        public abstract ReadOnlySpan<GraphicsDeviceInfo> AvailableDevices { get; }

        #endregion

        #region Constructors

        protected GraphicsManagement(OperatingSystem operatingSystem, DebugLevel debugLevel)
        {
            _operatingSystem = operatingSystem;
            _debugLevel = debugLevel;
        }

        // Override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~GraphicsManagement()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        #endregion

        #region Private Methods



        #endregion

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                // Set large fields to null
                _isDisposed = true;
            }
        }

        #endregion

        #region Public Methods

        public virtual GraphicsDeviceRequest GetAutoGraphicsDeviceRequest(IGraphicsView view)
        {
            ReadOnlySpan<GraphicsDeviceInfo> availableDevices = AvailableDevices;
            for (int i = 0; i < availableDevices.Length; i++)
                if (availableDevices[i].IsPresentSupported)
                {
                    uint[] supportedQueueGroups = availableDevices[i].GetCommandProcessorGroupIndicesSupportingView(view);
                    if (supportedQueueGroups.Length > 0)
                    {
                        ReadOnlySpan<GraphicsCommandProcessorGroupInfo> commandProcessorGroupInfos = availableDevices[i].CommandProcessorGroups;
                        for (int j = 0; j < supportedQueueGroups.Length; j++)
                            if (commandProcessorGroupInfos[j].Type.HasFlag(GraphicsCommandProcessorType.Graphics))
                                return new GraphicsDeviceRequest((uint)i, view, new PresentCommandProcessorRequest((uint)j, 0u));

                        //TODO: Handle different Present and Graphics queues!
                    }
                }

            throw new ApplicationException("None of the available Graphics Devices supports presenting!");
        }
        public virtual IGraphicsDevice CreateGraphicsDeviceAuto(IGraphicsView view)
            => CreateGraphicsDevice(GetAutoGraphicsDeviceRequest(view));
        public abstract IGraphicsDevice CreateGraphicsDevice(in GraphicsDeviceRequest deviceRequest);


        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }

    public class GraphicsManagementCreationException : Exception
    {

        public GraphicsManagementCreationException() { }
        public GraphicsManagementCreationException(string message) : base(message) { }
        public GraphicsManagementCreationException(string message, Exception innerException) : base(message, innerException) { }

    }

}
