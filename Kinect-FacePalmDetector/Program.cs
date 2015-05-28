using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect.VisualGestureBuilder;
using Microsoft.Kinect;

namespace Kinect_FacePalmDetector
{
    class Program
    {
        VisualGestureBuilderDatabase vgbd;
        VisualGestureBuilderFrameSource vgbfs;
        VisualGestureBuilderFrameReader vgbr;
        Gesture gesture;
        KinectSensor sensor;
        BodyFrameReader bfr;
        string vgbFolder = "VGBDatabase";               // this is where we've stored our database file (it's a .gbd)

        static void Main(string[] args)
        {
            Program prog = new Program();
            prog.Initialize();
            Console.Read();
        }

        private void Initialize()
        {

            sensor = KinectSensor.GetDefault();
            bfr = sensor.BodyFrameSource.OpenReader();
            bfr.FrameArrived += bfr_FrameArrived;
            vgbd = new VisualGestureBuilderDatabase(vgbFolder + @"\" + "FP.gbd");              // our .gbd file retrieved from the folder we put it in
            vgbfs = new VisualGestureBuilderFrameSource(KinectSensor.GetDefault(), 0);


            foreach (var g in vgbd.AvailableGestures)
            {
                if (g.Name.Equals("FP"))
                {
                    gesture = g;
                    vgbfs.AddGesture(gesture);
                }
            }
            vgbr = vgbfs.OpenReader();
            vgbfs.GetIsEnabled(gesture);
            vgbr.FrameArrived += vgbr_FrameArrived;
            sensor.Open();
        }
        void bfr_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            //Check to see if VGB has a valid tracking id, if not find a new body to track
            if (!vgbfs.IsTrackingIdValid)
            {

                using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
                {
                    if (bodyFrame != null)
                    {
                        Body[] bodies = new Body[6];
                        bodyFrame.GetAndRefreshBodyData(bodies);
                        Body closestBody = null;
                        //iterate through the bodies and pick the one closest to the camera
                        foreach (Body b in bodies)
                        {
                            if (b.IsTracked)
                            {
                                if (closestBody == null)
                                {
                                    closestBody = b;
                                }
                                else
                                {
                                    Joint newHeadJoint = b.Joints[JointType.Head];
                                    Joint oldHeadJoint = closestBody.Joints[JointType.Head];
                                    if (newHeadJoint.TrackingState == TrackingState.Tracked && newHeadJoint.Position.Z < oldHeadJoint.Position.Z)
                                    {
                                        closestBody = b;
                                    }
                                }
                            }
                        }

                        //if we found a tracked body, update the trackingid for vgb
                        if (closestBody != null)
                        {
                            vgbfs.TrackingId = closestBody.TrackingId;
                        }
                    }
                }
            }
        }


        void vgbr_FrameArrived(object sender, VisualGestureBuilderFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    //This check is almost certainly not needed for this sample, left in for debugging help
                    if (vgbfs.IsTrackingIdValid)
                    {
                        // get the discrete gesture results which arrived with the latest frame
                        IReadOnlyDictionary<Gesture, DiscreteGestureResult> discreteResults = frame.DiscreteGestureResults;

                        if (discreteResults != null)
                        {
                            // we only have one gesture in this source object, but you could get multiple gestures
                            foreach (Gesture gesture in this.vgbfs.Gestures)
                            {
                                // make sure we're matching the "FP" (FacePalm) gesture here
                                if (gesture.Name.Equals("FP") && gesture.GestureType == GestureType.Discrete)
                                {
                                    DiscreteGestureResult result = null;
                                    discreteResults.TryGetValue(gesture, out result);

                                    if (result != null)
                                    {
                                        // make sure we actually detected it (only use the first frame so we don't keep displaying message)
                                        if (result.Detected && result.FirstFrameDetected)
                                        {
                                            // confirm we got the result
                                            Console.WriteLine("Facepalm gesture detected!");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}