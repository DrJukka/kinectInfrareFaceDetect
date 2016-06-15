using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Microsoft.Samples.Kinect.InfraredBasics
{
    public delegate void DetectedFaces(Face[] faces, Emotion[] emotions);

    public class FaceDetector
    {
        public DetectedFaces DetectedFaces;
        /// <summary>
        /// Face service client
        /// </summary>
        private IFaceServiceClient _faceServiceClient = null;
        private EmotionServiceClient _emotionServiceClient = null;

        private bool _faceProcessing = false;

        public object ImageEncodingProperties { get; private set; }

        public FaceDetector(string facekey, string emotionKey)
        {
            // Initialize the face service client
            _faceServiceClient = new FaceServiceClient(facekey);
            _emotionServiceClient = new EmotionServiceClient(emotionKey);
        }

        /// <summary>
        /// Upload the frame and get the face detect result
        /// </summary>
        public async void DetectFaces(WriteableBitmap bitmap)
        {
            if (bitmap == null || _faceProcessing)
            {
                return;
            }

            _faceProcessing = true;

            try
            {
                 BitmapEncoder faceDetectEncoder = new JpegBitmapEncoder();
                 // create frame from the writable bitmap and add to encoder
                 faceDetectEncoder.Frames.Add(BitmapFrame.Create(bitmap));

                 MemoryStream imageFileStream = new MemoryStream();

                 faceDetectEncoder.Save(imageFileStream);
                 imageFileStream.Position = 0;

                 var faces = await _faceServiceClient.DetectAsync(imageFileStream);

                //continue getting emotions
                GetEmotionDetails(bitmap, faces);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DetectFaces exception : " + ex.Message);
                DoDelayTimer();
            }
        }

        private async void GetEmotionDetails(WriteableBitmap bitmap, Face[] faces)
        {
            if (bitmap != null)
            {
                try
                {
                    BitmapEncoder faceDetectEncoder = new JpegBitmapEncoder();
                    // create frame from the writable bitmap and add to encoder
                    faceDetectEncoder.Frames.Add(BitmapFrame.Create(bitmap));

                    MemoryStream imageFileStream = new MemoryStream();
                    faceDetectEncoder.Save(imageFileStream);

                    Emotion[] emotions = await _emotionServiceClient.RecognizeAsync(imageFileStream);

                    DetectedFaces?.Invoke(faces?.ToArray(), emotions?.ToArray());
                    DoDelayTimer();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("GetEmotionDetails exception : " + ex.Message);
                }
            }

            DetectedFaces?.Invoke(faces?.ToArray(),null);
            DoDelayTimer();
        }


        // to avoid asking too many queries, lets have a quick break after each query
        private void DoDelayTimer()
        {
            var timer = new DispatcherTimer();
            timer.Tick += (sender, args) =>
            {
                _faceProcessing = false;
                timer.Stop();
            };
            timer.Interval = new TimeSpan(0, 0, 0,1);
            timer.Start();
        }
    }
}
