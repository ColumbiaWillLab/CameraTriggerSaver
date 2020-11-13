/*=============================================================================
  Copyright (C) 2013 Allied Vision Technologies.  All Rights Reserved.

  Redistribution of this file, in original or modified form, without
  prior written consent of Allied Vision Technologies is prohibited.

-------------------------------------------------------------------------------

  File:        Program.cs

  Description: The main entry point of the AsynchronousGrabConsole example of VimbaNET.

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
    using System.IO;

    using AVT.VmbAPINET;

    /// <summary>
    /// The Program class 
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="cameraID">Camera ID to open - leave blank for first available</param>
        /// <param name="path">Image save folder path</param>
        /// <param name="shotSize">Number of pictures in a shot</param>
        /// <param name="GainAmt">Gain setting, default=0</param>
        private static void Main(string cameraID = null, string path = ".\\", int shotSize = 3, float GainAmt = 0)
        {
            VimbaHelper.FrameInfos showFrameInfos = VimbaHelper.FrameInfos.Show;           // Show frame info's

            Console.WriteLine("\n//////////////////////////////////////////////////////");
            Console.WriteLine("Vimba API Camera Trigger Saver (+ higher corrections)");
            Console.WriteLine("//////////////////////////////////////////////////////\n");

            try
            {
                // Create a new Vimba entry object
                VimbaHelper vimbaHelper = new VimbaHelper(path, shotSize, GainAmt);
                vimbaHelper.Startup(); // Startup API
                Console.WriteLine("Vimba .NET API Version {0}", vimbaHelper.GetVersion());

                // Open camera
                try
                {
                    if (null == cameraID)
                    {
                        // Open first available camera

                        // Fetch all cameras known to Vimba
                        List<Camera> cameras = vimbaHelper.CameraList;
                        if (cameras.Count < 0)
                        {
                            throw new Exception("No camera available.");
                        }

                        foreach (Camera currentCamera in cameras)
                        {
                            // Check if we can open the camera in full mode
                            VmbAccessModeType accessMode = currentCamera.PermittedAccess;
                            if (VmbAccessModeType.VmbAccessModeFull == (VmbAccessModeType.VmbAccessModeFull & accessMode))
                            {
                                // Now get the camera ID
                                cameraID = currentCamera.Id;
                                break;
                            }
                        }

                        if (null == cameraID)
                        {
                            throw new Exception("Could not open any camera.");
                        }
                    }

                    Console.WriteLine("Opening camera with ID: " + cameraID);

                    // Start the continuous image acquisition 
                    vimbaHelper.StartContinuousImageAcquisition(cameraID, showFrameInfos);

                    Console.WriteLine("Press <enter> to stop acquisition...");
                    Console.ReadKey();

                    // Stop the image acquisition
                    vimbaHelper.StopContinuousImageAcquisition();
                    Console.WriteLine("\nAcquisition stopped.");
                }
                finally
                {
                    // shutdown the vimba Api
                    vimbaHelper.Shutdown();
                }
            }
            catch (VimbaException ve)
            {
                // Output in case of a vimba Exception 
                Console.WriteLine(ve.Message);
                Console.WriteLine("Return Code: " + ve.ReturnCode.ToString() + " (" + ve.MapReturnCodeToString() + ")");
            }
            catch (Exception e)
            {
                // Output in case of a System.Exception
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("Press any Key to exit!");
            Console.ReadKey();
        }
    }
}