using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.GraphicsViews
{
    public interface IUserInputSource
    {

        #region Properties

        /// <summary>
        /// False (default): Input events are collected internally in the <see cref="IGraphicsView"/>. Events are triggered when <see cref="QueryInputEvents"/> function is called on the caller's thread.
        /// True: Input events are triggered immediatly from the UI thread (external syncronization required)
        /// </summary>
        bool ImmediateInputEvents { get; set; }

        /// <summary>
        /// <see cref="KeyboardKey"/>s in this collection will be handled and counsumed by the <see cref="IGraphicsView"/>. Input events of these Keys will not be propagated to other parent UI elements.
        /// </summary>
        IEnumerable<KeyboardKey>? HandleKeys { get; set; }
        /// <summary>
        /// <see cref="MouseButton"/>s in this collection will be handled and counsumed by the <see cref="IGraphicsView"/>. Input events of these Buttons will not be propagated to other parent UI elements.
        /// </summary>
        IEnumerable<MouseButton>? HandleMouseButtons { get; set; }
        /// <summary>
        /// When true the Mouse Wheel events will not be propagated to other parent UI elements. 
        /// </summary>
        bool HandleMouseWheel { get; set; }
        /// <summary>
        /// When true the Mouse Move events will not be propagated to other parent UI elements. 
        /// </summary>
        bool HandleMouseMove { get; set; }
        /// <summary>
        /// When true the Touch events will not be propagated to other parent UI elements. 
        /// </summary>
        bool HandleTouch { get; set; }

        #endregion

        #region Events

        /// <summary>
        /// Triggering is dependent on the value of <see cref="ImmediateInputEvents"/>.
        /// Each event indicates a single User Input on the User Interface. Uses derived classes of <see cref="UserInputEventArgs"/> to determine the kind of the input.
        /// </summary>
        event EventHandler<UserInputEventArgs> UserInput;

        #endregion

        #region Methods

        /// <summary>
        /// Triggers all collected input events.
        /// </summary>
        void QueryInputEvents();
        /// <summary>
        /// Discards all collected input events.
        /// </summary>
        void DiscardInputEvents();

        #endregion

    }
}
