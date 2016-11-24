using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTEX.Editor
{
    public class HighlightRanges
    {
        /////////////////////////////////////////////////////////////////////
        // EVENTS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Event triggered when a new range is added.
        /// </summary>
        public event Action<HighlightRanges, Range> OnAdded;

        /// <summary>
        /// Event triggered when the colour value of a range is changed.
        /// </summary>
        public event Action<HighlightRanges, Range> OnColourChanged;

        /// <summary>
        /// Event triggered when the colour value of a range is changed.
        /// </summary>
        public event Action<HighlightRanges, Range> OnSelectionChanged;

        /// <summary>
        /// Event triggered when any value of a range is changed.
        /// </summary>
        public event Action<HighlightRanges, Range> OnChanged;

        /// <summary>
        /// Event triggered when an existing range is removed (by updating it with the same start and end indices).
        /// Not fired when Clear() is called (use <see cref="OnCleared"/>).
        /// </summary>
        public event Action<HighlightRanges, Range> OnRemoved;

        /// <summary>
        /// Event triggered when the collection of ranges is cleared.
        /// </summary>
        public event Action<HighlightRanges> OnCleared;

        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        public class Range
        {
            public Guid ID { get; private set; }
            public int Index { get; private set; }
            public uint Start { get; private set; }
            public uint End { get; private set; }
            public Color Colour { get; private set; }
            public uint Low { get { return Start > End ? End : Start; } }
            public uint High { get { return Start < End ? End : Start; } }
            public uint Length { get { return (uint)((int)High - (int)Low); } }
            
            internal Range(Guid id, int i, uint s, uint e, Color c)
            {
                ID = id;
                Index = i;
                Update(s, e, c);
            }
            internal const int UpdatedSelection = 1;
            internal const int UpdatedColour = 2;
            internal int Update(uint s, uint e, Color c)
            {
                int result = 0;
                if (s != Start)
                {
                    Start = s;
                    result |= UpdatedSelection;
                }
                if (e != End)
                {
                    End = e;
                    result |= UpdatedSelection;
                }
                if (c != Colour)
                {
                    Colour = c;
                    result |= UpdatedColour;
                }
                return result;
            }
        }
        
        public Range[] Ranges
        {
            get
            {
                lock (ranges)
                {
                    var arr = rangesList.ToArray();
                    /*
                    Array.Sort(arr, (a,b) =>
                    {
                        return a.Index - b.Index;
                    });
                    */
                    return arr;
                }
            }
        }
        private readonly Dictionary<Guid, Range> ranges = new Dictionary<Guid, Range>();
        private readonly List<Range> rangesList = new List<Range>();
        private readonly Queue<int> indices = new Queue<int>();
        private readonly int capacity;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        public HighlightRanges(int capacity = 200)
        {
            this.capacity = capacity;
            for (int i = 0; i < capacity; ++i)
                indices.Enqueue(i);
        }

        /////////////////////////////////////////////////////////////////////
        // SETTING/UPDATING A RANGE
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Set/update a highlight range for a specific id.
        /// </summary>
        /// <param name="id">Unique id of range.</param>
        /// <param name="start">Range start index.</param>
        /// <param name="end">Range end index. If end == start, the range is empty and if one
        /// with the same ID already exists it will be deleted.</param>
        /// <param name="colour">Colour to use for highlighting</param>
        public void Set(Guid id, uint start, uint end, Color colour)
        {
            Range range = null;

            //deletions
            if (start == end)
            {
                lock (ranges)
                {
                    if (ranges.TryGetValue(id, out range))
                    {
                        ranges.Remove(id);
                        rangesList[range.Index] = null;
                        indices.Enqueue(range.Index);
                        OnRemoved?.Invoke(this, range);
                    }
                }
                return;
            }

            //additions
            lock (ranges)
            {
                if (!ranges.TryGetValue(id, out range))
                {
                    if (ranges.Count >= capacity)
                        return;

                    var index = indices.Dequeue();
                    while (rangesList.Count < (index + 1))
                        rangesList.Add(null);

                    ranges[id] = rangesList[index] = range = new Range(id, index, start, end, colour);
                    OnAdded?.Invoke(this, range);
                    return;
                }
            }

            //updates
            var changeResult = range.Update(start, end, colour);
            if (changeResult != 0)
            {
                if ((changeResult & Range.UpdatedColour) != 0)
                    OnColourChanged?.Invoke(this, range);
                if ((changeResult & Range.UpdatedSelection) != 0)
                    OnSelectionChanged?.Invoke(this, range);
                OnChanged?.Invoke(this, range);
            }
        }

        /// <summary>
        /// Clears all custom highlight ranges.
        /// </summary>
        public void Clear()
        {
            if (ranges.Count > 0)
            {
                lock (ranges)
                {
                    if (ranges.Count > 0)
                    {
                        ranges.Clear();
                        rangesList.Clear();
                        indices.Clear();
                        for (int i = 0; i < capacity; ++i)
                            indices.Enqueue(i);
                        OnCleared?.Invoke(this);
                    }
                }
            }
        }
    }
}
