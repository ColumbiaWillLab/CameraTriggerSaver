/*=============================================================================
  Adapted from Allied Vision AsynchronousGrabForms example
 -------------------------------------------------------------------------------
  File:        RingBitmap.cs
  Description: Implementation file for the RingBitmap class.
               Contains a configurable ring bitmap array.
               Each bitmap will only be created one time and reused afterwards.
 -------------------------------------------------------------------------------
 =============================================================================*/

namespace CameraTriggerSaver
{
    using System.Drawing;
    using AVT.VmbAPINET;

    /// <summary>
    /// Helper class to provide necessary bitmap functions
    /// A ring buffer of bitmaps
    /// </summary>
    class RingBitmap
    {
        /// <summary>
        /// The bitmap size
        /// </summary>
        private readonly int _Size = 0;

        /// <summary>
        /// Bitmaps to display images
        /// </summary>
        private readonly Bitmap[] _Bitmaps = null;

        /// <summary>
        /// selects Bitmap
        /// </summary>
        private int _BitmapSelector = 0;

        /// <summary>
        /// Initializes a new instance of the RingBitmap class.
        /// </summary>
        /// <param name="size">The bitmap size</param>
        public RingBitmap(int size)
        {
            _Size = size;
            _Bitmaps = new Bitmap[_Size];
        }

        /// <summary>
        /// Gets the current bitmap as image
        /// </summary>
        public Image Image { get => _Bitmaps[_BitmapSelector]; }

        /// <summary>
        /// Fill Frame in 8bppIndexed bitmap
        /// </summary>
        /// <param name="frame">The Vimba frame</param>
        public void FillNextBitmap(Frame frame)
        {
            // switch to Bitmap object which is currently not in use by GUI
            SwitchBitmap();

            frame.Fill(ref _Bitmaps[_BitmapSelector]);
        }

        /// <summary>
        /// Bitmap rotation selector
        /// </summary>
        private void SwitchBitmap()
        {
            _BitmapSelector++;

            if (_Size == _BitmapSelector)
            {
                _BitmapSelector = 0;
            }
        }
    }
}
