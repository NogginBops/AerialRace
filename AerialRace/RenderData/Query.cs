using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace.RenderData
{
    public enum QueryType
    {
        TimeElapsed = 1,
        Timestamp = 2,
    }

    public class BufferedQuery
    {
        public string Name;
        public QueryType Type;
        public int[] StartHandles;
        public int[] EndHandles;
        public int CurrentQuery = 0;
        public int ReadQuery = 0;

        public BufferedQuery(string name, QueryType type, int[] startHandles, int[] endHandles)
        {
            Name = name;
            Type = type;
            StartHandles = startHandles;
            EndHandles = endHandles;
            ReadQuery = startHandles.Length - 1;
        }

        public override string ToString()
        {
            return $"{Name} (Current: {CurrentQuery}, Read: {ReadQuery})";
        }
    }
}
