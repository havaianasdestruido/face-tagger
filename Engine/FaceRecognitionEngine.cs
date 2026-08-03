using System;
using System.Runtime.InteropServices;

namespace FaceTagger.Engine
{
    internal static class Hr
    {
        public const int CLASS_E_CLASSNOTAVAILABLE = unchecked((int)0x80040111);
        public static string Name(int hr) => hr switch
        {
            0 => "S_OK",
            CLASS_E_CLASSNOTAVAILABLE => "CLASS_E_CLASSNOTAVAILABLE",
            _ => $"0x{(uint)hr:X8}"
        };
    }

    internal static class Native
    {
        // Minimal probe: call DllCanUnloadNow, always succeeds if DLL loads.
        [DllImport("WLXFaceRecognition.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern int DllCanUnloadNow();
    }

    public class FaceRecognitionEngine
    {
        public int ProbeResult { get; private set; }
        public string ProbeMessage { get; private set; }

        public FaceRecognitionEngine()
        {
            try
            {
                // Ensure DLL directory is in path.
                var dllDir = @"C:\Users\mcmco\Desktop\WMMR\build_clean\bin\Debug";
                var old = Environment.CurrentDirectory;
                Environment.CurrentDirectory = dllDir;
                ProbeResult = Native.DllCanUnloadNow();
                Environment.CurrentDirectory = old;
            }
            catch (DllNotFoundException ex)
            {
                ProbeResult = Hr.CLASS_E_CLASSNOTAVAILABLE;
            }
            ProbeMessage = $"Engine probe HRESULT: {ProbeResult} ({Hr.Name(ProbeResult)})";
        }
    }
}
