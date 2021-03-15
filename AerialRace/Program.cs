using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace AerialRace
{
    // FIXME: Move this somewhere else
    class DebugThing : TraceListener
    {
        readonly TextWriter Writer = new StreamWriter(new FileStream("./log.log", FileMode.Create));
        
        public override void Write(string? message)
        {
            Writer.Write(message);
            Writer.Flush();
        }

        public override void WriteLine(string? message)
        {
            Writer.WriteLine(message);
            Writer.Flush();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Trace.Listeners.Add(new DebugThing());

            /*
            {
                Random rand = new Random();
                Debug.WriteLine($"vec3[] {{");
                int N = 20;
                for (int i = 0; i < N; i++)
                {
                    var dir = rand.NextOnUnitSphere();
                    dir.Y = MathF.Abs(dir.Y);

                    Debug.WriteLine($"    vec3{dir},");
                }
                Debug.WriteLine($"}}");
            }
            */

            GameWindowSettings gwSettings = new GameWindowSettings()
            {
                IsMultiThreaded = false,
                RenderFrequency = 0,
                UpdateFrequency = 0,
            };

            NativeWindowSettings nwSettings = new NativeWindowSettings()
            {
                API = ContextAPI.OpenGL,
                //APIVersion = 
                AutoLoadBindings = true,
                //CurrentMonitor = 
                Flags = ContextFlags.Debug | ContextFlags.ForwardCompatible,
                //Icon = 
                IsEventDriven = false,
                IsFullscreen = false,
                //Location =
                // TODO: We will probably just use FBO multisampling
                //NumberOfSamples = 4,
                Profile = ContextProfile.Core,
                SharedContext = null,
                Size = new Vector2i(1920, 1080),
                StartFocused = true,
                StartVisible = true,
                Title = "AerialRacer",
                //WindowBorder = WindowBorder.Fixed,
                WindowState = WindowState.Normal,
            };

            using Window window = new Window(gwSettings, nwSettings);
            window.Run();
        }
    }
}
