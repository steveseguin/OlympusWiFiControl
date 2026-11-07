// Compile together with the production UDPInputStream.cs. The UI and Picture
// doubles keep this sequence test independent of WPF, JPEG decoding and sockets.
namespace DebugShellTest
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public static class SequenceRegression
    {
        private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo Counter = typeof(UDPInputStream).GetField("UDPackageNumberCounter", PrivateInstance);
        private static readonly FieldInfo CurrentPicture = typeof(UDPInputStream).GetField("pic", PrivateInstance);
        private static readonly MethodInfo Receive = typeof(UDPInputStream).GetMethod("analyseData", PrivateInstance);

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void Send(UDPInputStream stream, int sequence)
        {
            Receive.Invoke(stream, new object[] { new byte[] { 0x80, 0x60, (byte)(sequence >> 8), (byte)(sequence & 255) } });
        }

        public static string Run()
        {
            // Every possible low-byte transition, especially 254 -> 255 and 255 -> 0.
            for (int previous = 0; previous < 256; previous++)
            {
                var stream = new UDPInputStream(0);
                Counter.SetValue(stream, (byte)previous);
                int next = (previous + 1) & 255;
                Send(stream, next);
                var picture = (Picture)CurrentPicture.GetValue(stream);
                Assert(picture != null && picture.Sequences.Count == 1 && picture.Sequences[0] == next,
                    "Rejected in-order transition " + previous + " -> " + next);
            }

            // Keep the same picture across multiple low-byte wraps and one full RTP wrap.
            var continuous = new UDPInputStream(0);
            Counter.SetValue(continuous, (byte)252);
            Picture first = null;
            for (int sequence = 65533; sequence <= 66048; sequence++)
            {
                Send(continuous, sequence & 65535);
                var picture = (Picture)CurrentPicture.GetValue(continuous);
                if (first == null) first = picture;
                Assert(picture != null && Object.ReferenceEquals(first, picture),
                    "Discarded continuous picture at sequence " + sequence);
            }
            Assert(first.Sequences.Count == 516, "Continuous packets were lost or duplicated");

            // A gap must still discard the partial picture and resynchronize.
            var gap = new UDPInputStream(0);
            Send(gap, 1);
            var beforeGap = (Picture)CurrentPicture.GetValue(gap);
            Send(gap, 3);
            Assert(CurrentPicture.GetValue(gap) == null, "Gap retained a partial picture");
            Assert((byte)Counter.GetValue(gap) == 3, "Gap did not resynchronize the counter");
            Send(gap, 4);
            var afterGap = (Picture)CurrentPicture.GetValue(gap);
            Assert(afterGap != null && !Object.ReferenceEquals(beforeGap, afterGap), "Gap did not start a fresh picture");
            Assert(afterGap.Sequences.Count == 1 && afterGap.Sequences[0] == 4, "Packet after gap was not accepted once");
            Send(gap, 4);
            Assert(CurrentPicture.GetValue(gap) == null, "Duplicate packet was accepted");
            Send(gap, 5);
            Assert(((Picture)CurrentPicture.GetValue(gap)).Sequences.Count == 1, "Duplicate recovery failed");

            return "Passed: 256 transitions, 516 continuous packets, gap and duplicate recovery.";
        }
    }

    class Picture
    {
        public readonly List<int> Sequences = new List<int>();
        public bool imageCompleted = false;
        public Picture(int number) { }
        public void addData(byte[] input) { Sequences.Add(input[3]); }
        public System.Windows.Media.Imaging.BitmapImage getImage() { return null; }
    }
}

// Only the unused UI surface referenced by UDPInputStream is stubbed here.
namespace System.Windows
{
    public class Application
    {
        public static Application Current { get { return null; } }
        public System.Windows.Threading.Dispatcher Dispatcher { get { return null; } }
    }
}
namespace System.Windows.Threading
{
    public enum DispatcherPriority { Background }
    public class Dispatcher { public void Invoke(DispatcherPriority priority, System.Threading.ThreadStart action) { } }
}
namespace System.Windows.Media.Imaging { public class BitmapImage { } }
namespace System.Windows.Controls { }
namespace System.Windows.Data { }
namespace System.Windows.Documents { }
namespace System.Windows.Input { }
namespace System.Windows.Media { }
namespace System.Windows.Navigation { }
namespace System.Windows.Shapes { }
