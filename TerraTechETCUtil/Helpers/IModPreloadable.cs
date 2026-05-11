using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraTechETCUtil
{
    /// <summary>
    /// For preloading things on the title screen 
    /// </summary>
    public interface IModPreloadable
    {
        /// <summary>
        /// The handle of the mod incase it fails
        /// </summary>
        ModDataHandle ModHandle { get; }
        /// <summary>
        /// If this fails, abort all other mods loading
        /// </summary>
        bool ChainFail { get; }
        /// <summary>
        /// Called when it fails
        /// </summary>
        void OnFail();


        /// <summary>
        /// The name of the subject to display
        /// </summary>
        string Subject { get; }
        /// <summary>
        /// String to display on the screen whilist this is in progress
        /// </summary>
        string InProgress { get; }
        /// <summary>
        /// Estimated percent done.  Usually <see cref="EstNumSteps"/> / <see cref="EstNumStepsIterator"/>
        /// </summary>
        float EstPercentDone { get; }
        /// <summary>
        /// The estimated number of steps this has to go through to display on the progress bar.
        /// <para>Leave at 0 to hide</para>
        /// </summary>
        int EstNumSteps { get; }
        /// <summary>
        /// The estimated number of steps this has already gon through to display on the progress bar
        /// </summary>
        int EstNumStepsIterator { get; }
        /// <summary>
        /// The iterator function to run whilist this is in the queue.
        /// </summary>
        /// <returns></returns>
        IEnumerator GetEnumerator();
    }
}
