using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AerialRace.RenderData;

namespace AerialRace.Editor
{
    struct ProfileEntry
    {
        public string Name;
        public long Timestamp;
        public long Duration;
        public int ParentIndex;

        public double DurationInMilliseconds => (Duration / (double)Stopwatch.Frequency) * 1000d;

        public override string ToString()
        {
            return $"{Name}: {DurationInMilliseconds:0.000}ms";
        }
    }

    class RenderPassInfo
    {
        public struct TimeInfo
        {
            public double Min, Max;
            public double Sum;
            public double[] Values;
            public int Index;

            public double Average => Sum / Values.Length;

            public void Add(double time)
            {
                if (time < Min) Min = time;
                if (time > Max) Max = time;

                Values[Index] = time;
                Index = (Index + 1) % Values.Length;

                Sum -= Values[Index];
                Sum += time;
            }

            public override string ToString()
            {
                return $"Min:{Min:0.000}ms|Max:{Max:0.000}ms|Average:{Average:0.000}ms";
            }
        }

        public string Name;
        public int ParentID;
        public bool ActiveThisFrame;
        public BufferedQuery Query;
        public TimeInfo CpuTime;
        public TimeInfo GpuTime;

        public RenderPassInfo(string name, int parentID, BufferedQuery query, int movingAverageSamples)
        {
            Name = name;
            ParentID = parentID;
            Query = query;
            
            CpuTime.Min = double.PositiveInfinity;
            CpuTime.Max = double.NegativeInfinity;
            CpuTime.Values = new double[movingAverageSamples];
            CpuTime.Index = 0;

            GpuTime.Min = double.PositiveInfinity;
            GpuTime.Max = double.NegativeInfinity;
            GpuTime.Values = new double[movingAverageSamples];
            GpuTime.Index = 0;
        }

        public override string ToString()
        {
            return $"{Name}{{CPU: {CpuTime}, GPU: {GpuTime}}}";
        }
    }

    class ThreadData
    {
        public RefList<ProfileEntry> Entries = new RefList<ProfileEntry>();
        public Stack<int> ParentIndices = new Stack<int>();

        public List<RenderPassInfo> CurrentFramePasses = new List<RenderPassInfo>();
        public List<RenderPassInfo> PreviousFramePasses = new List<RenderPassInfo>();

        public Dictionary<int, RenderPassInfo> PassInfos = new Dictionary<int, RenderPassInfo>();
    }

    static class Profiling
    {
        public const int TimeQueryBufferLength = 4;
        public const int MovingAverageSamples = 200;

        public static long StartOfFrameTimestamp;
        public static System.Threading.ThreadLocal<ThreadData> ProfilerData = new System.Threading.ThreadLocal<ThreadData>();

        public static ThreadData EnsureInitedOnThread()
        {
            if (ProfilerData.Value == null)
            {
                ProfilerData.Value = new ThreadData();
            }
            
            return ProfilerData.Value;
        }

        public static int CurrentParentIndex()
        {
            var data = EnsureInitedOnThread();

            return data.Entries.Count - 1;
        }

        public static void /*int*/ PushSpan(string name, int passID)
        {
            var data = EnsureInitedOnThread();

            // Create the entry and add it to the list
            ProfileEntry entry;
            entry.Name = name;

            entry.Timestamp = Stopwatch.GetTimestamp();
            entry.Duration = -1;

            if (data.ParentIndices.TryPeek(out int parent))
                entry.ParentIndex = parent;
            else entry.ParentIndex = -1;

            data.Entries.Add(entry);

            int id = data.Entries.Count - 1;

            // Push the id if this entry onto the stack.
            data.ParentIndices.Push(id);

            if (data.PassInfos.TryGetValue(passID, out var passInfo) == false)
            {
                var query = RenderDataUtil.CreateQuery(name, QueryType.Timestamp, TimeQueryBufferLength);
                passInfo = new RenderPassInfo(name, entry.ParentIndex, query, MovingAverageSamples);
                data.PassInfos.Add(passID, passInfo);
            }

            passInfo.ActiveThisFrame = true;

            data.CurrentFramePasses.Add(passInfo);

            RenderDataUtil.BeginQuery(passInfo.Query);

            //return id;
        }

        public static void PopSpan(int passID)
        {
            var data = EnsureInitedOnThread();

            // Pop this entry's id from the stack.
            var stackID = data.ParentIndices.Pop();
            //Debugging.Debug.Assert(stackID == id);

            // Set the duration of the entry
            ref var entry = ref data.Entries[stackID];
            entry.Duration = Stopwatch.GetTimestamp() - entry.Timestamp;

            var passInfo = data.PassInfos[passID];
            passInfo.CpuTime.Add(entry.DurationInMilliseconds);

            RenderDataUtil.EndQuery(passInfo.Query);
        }

        // FIXME: Do something here for all threads instead of just the thread that called this function
        public static void NewFrame()
        {
            StartOfFrameTimestamp = Stopwatch.GetTimestamp();

            var data = EnsureInitedOnThread();

            data.Entries.Clear();
            data.ParentIndices.Clear();

            // Make the recorded entrires appear in the PreviousFramePasses list.
            var temp = data.PreviousFramePasses;
            data.PreviousFramePasses = data.CurrentFramePasses;
            data.CurrentFramePasses = temp;

            // Clear this list in anticipacion for this frame.
            data.CurrentFramePasses.Clear();

            if (data.PreviousFramePasses.Count != 0)
            {
                var last = data.PreviousFramePasses[^1];

                // FIXME: Don't loop like this if we can avoid it
                long start = Stopwatch.GetTimestamp();
                while (RenderDataUtil.IsQueryReady(last.Query) == false)
                { }
                long end = Stopwatch.GetTimestamp();

                double time = (end - start) / (double)Stopwatch.Frequency;
                //Debug.WriteLine($"Waiting for queries took: {time * 1000:0.000ms}");

                for (int i = 0; i < data.PreviousFramePasses.Count; i++)
                {
                    var pass = data.PreviousFramePasses[i];
                    var nanoSeconds = RenderDataUtil.GetTimeElapsedQueryResult(pass.Query, false);
                    pass.GpuTime.Add((nanoSeconds / 1_000_000_000d) * 1000d);
                }
            }
            
            /*
            foreach (var (_, info) in data.PassInfos)
            {
                info.ActiveThisFrame = false;
            }
            */
        }
    }
}
