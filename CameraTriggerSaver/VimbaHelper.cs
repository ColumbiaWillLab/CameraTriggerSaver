/*=============================================================================
  Copyright (C) 2012 - 2016 Allied Vision Technologies.  All Rights Reserved.

  Redistribution of this file, in original or modified form, without
  prior written consent of Allied Vision Technologies is prohibited.

-------------------------------------------------------------------------------

  File:        VimbaHelper.cs

  Description: Implementation file for the VimbaHelper class that demonstrates
               how to implement an asynchronous, continuous image acquisition
               with VimbaNET.

-------------------------------------------------------------------------------

  THIS SOFTWARE IS PROVIDED BY THE AUTHOR "AS IS" AND ANY EXPRESS OR IMPLIED
  WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF TITLE,
  NON-INFRINGEMENT, MERCHANTABILITY AND FITNESS FOR A PARTICULAR  PURPOSE ARE
  DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT,
  INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
  (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
  LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED
  AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR
  TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
  OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

=============================================================================*/

namespace CameraTriggerSaver
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using AVT.VmbAPINET;

    /// <summary>
    /// A helper class as a wrapper around Vimba
    /// </summary>
    public class VimbaHelper
    {
        private Vimba m_Vimba = null;                           // Main Vimba API entry object
        private Camera m_Camera = null;                         // Camera object if camera is open
        private bool m_Acquiring = false;                       // Flag to remember if acquisition is running
        private FrameInfos m_ShowFrameInfo = FrameInfos.Off;    // Indicates if frame info is shown in the console
        private ulong m_FrameID = 0;
        private string m_Path = null;
        private int m_shotSize = 3;
        private float m_GainValue = 0;
        private int m_shotCounter = 0;
        private DateTime m_shotTime = DateTime.Now;
        private static RingBitmap m_RingBitmap;


        /// <summary>
        /// Initializes a new instance of the VimbaHelper class
        /// </summary>
        public VimbaHelper(string path, int shotSize, float GainAmt)
        {
            m_Path = path;
            m_shotSize = shotSize;
            m_GainValue = GainAmt;
            m_RingBitmap = new RingBitmap(shotSize);

        }

        /// <summary>
        /// Finalizes an instance of the VimbaHelper class and shuts down Vimba
        /// </summary>
        ~VimbaHelper()
        {
            // Release Vimba API if user forgot to call Shutdown
            ReleaseVimba();
        }

        /// <summary>
        /// Frame Infos Enumeration
        /// </summary>
        public enum FrameInfos
        {
            /// <summary>
            /// Do not Show frame information
            /// </summary>
            Off = 0,

            /// <summary>
            /// Show Frame information
            /// </summary>
            Show,

            /// <summary>
            /// Show frame information, if a frame is lost or a frame is not complete
            /// </summary>
            Automatic
        }

        /// <summary>
        /// Gets the current camera list
        /// </summary>
        public List<Camera> CameraList
        {
            get
            {
                // Check if API has been started up at all
                if (null == m_Vimba)
                {
                    throw new Exception("Vimba is not started.");
                }

                List<Camera> cameraList = new List<Camera>();
                CameraCollection cameras = m_Vimba.Cameras;
                foreach (Camera camera in cameras)
                {
                    cameraList.Add(camera);
                }

                return cameraList;
            }
        }

        /// <summary>
        /// Starts up Vimba API and loads all transport layers
        /// </summary>
        public void Startup()
        {
            // Instantiate main Vimba object
            Vimba vimba = new Vimba();

            // Start up Vimba API
            vimba.Startup();
            m_Vimba = vimba;
        }

        /// <summary>
        /// Shuts down Vimba API
        /// </summary>
        public void Shutdown()
        {
            // Check if API has been started up at all
            if (null == m_Vimba)
            {
                throw new Exception("Vimba has not been started.");
            }

            ReleaseVimba();
        }

        /// <summary>
        /// Returns the version of Vimba API
        /// </summary>
        /// <returns></returns>
        public String GetVersion()
        {
            if (null == m_Vimba)
            {
                throw new Exception("Vimba has not been started.");
            }
            VmbVersionInfo_t version_info = m_Vimba.Version;
            return String.Format("{0:D}.{1:D}.{2:D}", version_info.major, version_info.minor, version_info.patch);
        }

        /// <summary>
        /// Starts the continuous image acquisition and opens the camera
        /// Registers the event handler for the new frame event
        /// </summary>
        /// <param name="id">The camera ID</param>
        /// <param name="showCameraInfo">Flag to indicate if the Camera info data shall be shown or not</param>
        public void StartContinuousImageAcquisition(string id, FrameInfos showCameraInfo)
        {
            // Check parameters
            if (null == id)
            {
                throw new ArgumentNullException("id");
            }

            // Check if API has been started up at all
            if (null == m_Vimba)
            {
                throw new Exception("Vimba is not started.");
            }

            // Check if a camera is already open
            if (null != m_Camera)
            {
                throw new Exception("A camera is already open.");
            }

            // show camera info's in the console Output :
            m_ShowFrameInfo = showCameraInfo;

            // Open camera
            m_Camera = m_Vimba.OpenCameraByID(id, VmbAccessModeType.VmbAccessModeFull);
            if (null == m_Camera)
            {
                throw new NullReferenceException("No camera retrieved.");
            }

            // Set the GeV packet size to the highest possible value
            // (In this example we do not test whether this cam actually is a GigE cam)
            try
            {
                m_Camera.Features["GVSPAdjustPacketSize"].RunCommand();
                while (false == m_Camera.Features["GVSPAdjustPacketSize"].IsCommandDone()) { }
            }
            catch
            {
                // Do nothing
            }

            bool error = true;
            try
            {
                Console.WriteLine("\nConfiguring Camera Parameters...");
                //Console.Write("Available Triggers: ");
                //Console.WriteLine(string.Join(", ", m_Camera.Features["TriggerSource"].EnumValues));

                m_Camera.Features["ExposureMode"].EnumValue = "TriggerWidth";
                m_Camera.Features["TriggerSelector"].EnumValue = "FrameStart";
                m_Camera.Features["TriggerSource"].EnumValue = "Line1";
                m_Camera.Features["TriggerActivation"].EnumValue = "RisingEdge";
                m_Camera.Features["TriggerMode"].EnumValue = "On";
                m_Camera.Features["Gain"].FloatValue = m_GainValue;

                Console.WriteLine("ExposureMode: " + m_Camera.Features["ExposureMode"].EnumValue);
                Console.WriteLine("TriggerSelector: " + m_Camera.Features["TriggerSelector"].EnumValue);
                Console.WriteLine("TriggerSource: " + m_Camera.Features["TriggerSource"].EnumValue);
                Console.WriteLine("TriggerActivation: " + m_Camera.Features["TriggerActivation"].EnumValue);
                Console.WriteLine("TriggerMode: " + m_Camera.Features["TriggerMode"].EnumValue);
                Console.WriteLine("Gain: " + m_Camera.Features["Gain"].FloatValue);
                Console.WriteLine("Saving to: " + m_Path);

                Console.WriteLine("\nReady for imaging!\n");

                // Register frame callback
                m_Camera.OnFrameReceived += this.OnFrameReceived;

                // Reset member variables
                m_Acquiring = true;

                // Start synchronous image acquisition (grab)
                m_Camera.StartContinuousImageAcquisition(m_shotSize);

                error = false;
            }
            finally
            {
                // Close camera already if there was an error
                if (true == error)
                {
                    ReleaseCamera();
                }
            }
        }

        /// <summary>
        /// Stops the image acquisition
        /// </summary>
        public void StopContinuousImageAcquisition()
        {
            // Check if API has been started up at all
            if (null == m_Vimba)
            {
                throw new Exception("Vimba is not started.");
            }

            // Check if no camera is open
            if (null == m_Camera)
            {
                throw new Exception("No camera open.");
            }

            // Close camera
            ReleaseCamera();
        }

        /// <summary>
        /// Display the image as dots or show the FrameInfo
        /// </summary>
        /// <param name="frame">The received frame</param>
        private void OutPutFrameInfo(Frame frame)
        {
            bool showFrameInfo = false;

            if (null == frame)
            {
                throw new ArgumentNullException("frame");
            }
            string status = GetFrameStatus(frame); // Get frame status

            ulong nFramesMissing = GetFramesMissing(frame);
            if (0 < nFramesMissing)
            {
                if (1 == nFramesMissing)
                {
                    Console.WriteLine("1 missing frame detected\n");
                }
                else
                {
                    Console.WriteLine("{0} missing frames detected\n", nFramesMissing);
                }
            }

            showFrameInfo = status != "Complete" || 0 < nFramesMissing;  // Show frame infos for incomplete frame or if a frame is lost

            if (m_ShowFrameInfo == FrameInfos.Show || (showFrameInfo && m_ShowFrameInfo == FrameInfos.Automatic))
            {
                if (m_ShowFrameInfo == FrameInfos.Automatic)
                    Console.WriteLine(string.Empty);

                Console.Write("FrameID:");
                Console.Write(frame.FrameID);

                Console.Write(" Status:");
                Console.Write(status);

                Console.Write(" Size:");
                Console.Write(frame.Width);

                Console.Write("x");
                Console.Write(frame.Height);

                Console.Write(" Format:");
                Console.WriteLine(frame.PixelFormat);
            }
            else
            {
                Console.Write(".");
            }
        }

        /// <summary>
        /// Checks if a frame is missing
        /// </summary>
        /// <param name="frame">The received frame</param>
        /// <returns>True if a frame is missing</returns>
        private ulong GetFramesMissing(Frame frame)
        {
            ulong missingFrames;
            // For USB the very first frame ID is 0
            if (frame.FrameID == 0 && m_FrameID == 0)
            {
                missingFrames = 0;
            }
            else if (frame.FrameID - m_FrameID == 1)
            {
                missingFrames = 0;
            }
            else
            {
                missingFrames = frame.FrameID - m_FrameID;
            }

            m_FrameID = frame.FrameID;

            return missingFrames;
        }

        /// <summary>
        /// Gets the frame Status as a string
        /// </summary>
        /// <param name="frame">The received frame</param>
        /// <returns>Frame Status string</returns>
        private string GetFrameStatus(Frame frame)
        {
            string status = string.Empty;

            switch (frame.ReceiveStatus)
            {
                case VmbFrameStatusType.VmbFrameStatusComplete:
                    status = "Complete";
                    break;

                case VmbFrameStatusType.VmbFrameStatusIncomplete:
                    status = "Incomplete";
                    break;

                case VmbFrameStatusType.VmbFrameStatusTooSmall:
                    status = "Too small";
                    break;

                case VmbFrameStatusType.VmbFrameStatusInvalid:
                    status = "Invalid";
                    break;

                default:
                    status = "?";
                    break;
            }

            return status;
        }

        /// <summary>
        /// Handles the frame received event
        /// The frame is displayed and eventually queued
        /// </summary>
        /// <param name="frame">The received frame</param>
        private void OnFrameReceived(Frame frame)
        {
            try
            {
                if (m_shotCounter == 0 || m_shotCounter > m_shotSize)
                {
                    double temp = m_Camera.Features["DeviceTemperature"].FloatValue;
                    m_shotCounter = 1;
                    m_shotTime = DateTime.Now;
                    Console.WriteLine("\nShot: " + m_shotTime.ToString("s"));
                    Console.WriteLine("Temp: " + temp.ToString());
                    if (temp >= 55)
                    {
                        Console.WriteLine("***********TEMPERATURE WARNING***********");
                    }
                }

                // Convert frame into displayable image
                OutPutFrameInfo(frame);

                m_RingBitmap.FillNextBitmap(frame);
                Image img = m_RingBitmap.Image;
                /// Conditional renaming depending on shot #. Only 3 and 4 shots considered
                if (m_shotSize == 3)
                {
                    string path = Path.Combine(m_Path, m_shotTime.ToString("yyyy-MM-ddTHHmmss") + "-" + m_shotCounter.ToString() + ".bmp");
                    Console.WriteLine(path.ToString());
                    img.Save(path);
                }
                else if (m_shotSize == 4)
                {
                    int m_newshotCounter = m_shotCounter - 1;
                    string path = Path.Combine(m_Path, m_shotTime.ToString("yyyy-MM-ddTHHmmss") + "-" + m_newshotCounter.ToString() + ".bmp");
                    Console.WriteLine(path.ToString());
                    img.Save(path);
                }
                else
                {
                    string path = Path.Combine(m_Path, m_shotTime.ToString("yyyy-MM-ddTHHmmss") + "-" + m_shotCounter.ToString() + ".bmp");
                    Console.WriteLine(path.ToString());
                    img.Save(path);
                }

                //Console.WriteLine(path.ToString());
                //img.Save(path);
            }
            finally
            {
                // We make sure to always return the frame to the API
                try
                {
                    m_shotCounter++;
                    m_Camera.QueueFrame(frame);
                }
                catch
                {
                    // Do nothing
                }
            }
        }

        /// <summary>
        /// Convert frame to displayable or savable image
        /// </summary>
        /// <param name="frame">The received frame</param>
        /// <returns>The taken Image</returns>
        private static Image ConvertFrame(Frame frame)
        {
            if (null == frame)
            {
                throw new ArgumentNullException("frame");
            }

            // Check if the image is valid
            if (VmbFrameStatusType.VmbFrameStatusComplete != frame.ReceiveStatus)
            {
                throw new Exception("Invalid frame received. Reason: " + frame.ReceiveStatus.ToString());
            }

            // Convert raw frame data into
            Bitmap bitmap = new Bitmap((int)frame.Width, (int)frame.Height, PixelFormat.Format8bppIndexed);

            frame.Fill(ref bitmap);

            return bitmap;
        }

        /// <summary>
        ///  Unregisters the new frame event
        ///  Stops the capture engine
        ///  Closes the camera
        /// </summary>
        private void ReleaseCamera()
        {
            if (null != m_Camera)
            {
                // We can use cascaded try-finally blocks to release the
                // camera step by step to make sure that every step is executed.
                try
                {
                    try
                    {
                        try
                        {
                            m_Camera.OnFrameReceived -= this.OnFrameReceived;
                        }
                        finally
                        {
                            if (true == m_Acquiring)
                            {
                                m_Camera.StopContinuousImageAcquisition();
                            }
                        }
                    }
                    finally
                    {
                        m_Acquiring = false;
                        m_Camera.Close();
                    }
                }
                finally
                {
                    m_Camera = null;
                }
            }
        }

        /// <summary>
        ///  Releases the camera
        ///  Shuts down Vimba
        /// </summary>
        private void ReleaseVimba()
        {
            if (null != m_Vimba)
            {
                // We can use cascaded try-finally blocks to release the
                // Vimba API step by step to make sure that every step is executed.
                try
                {
                    try
                    {
                        // First we release the camera (if there is one)
                        ReleaseCamera();
                    }
                    finally
                    {
                        // Now finally shutdown the API
                        m_Vimba.Shutdown();
                    }
                }
                finally
                {
                    m_Vimba = null;
                }
            }
        }
    }
}