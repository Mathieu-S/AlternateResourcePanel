using System.Collections.Generic;

namespace KSPAlternateResourcePanel
{
    /// <summary>
    ///     A Queue structure that has a numerical limit.
    ///     When the limit is reached the oldest entry is discarded
    /// </summary>
    internal class LimitedQueue<T> : Queue<T>
    {
        private int limit = -1;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="limit">How many items the queue can hold</param>
        public LimitedQueue(int limit)
            : base(limit)
        {
            Limit = limit;
        }

        public int Limit
        {
            get => limit;
            set
            {
                limit = value;
                if (limit > 0)
                    //If more items in the queue than the limit then dequeue stuff
                    while (Count > limit)
                        Dequeue();
            }
        }

        /// <summary>
        ///     Add a new item to the queue. If this would exceed the limit then the oldest item is discarded
        /// </summary>
        /// <param name="item"></param>
        public new void Enqueue(T item)
        {
            if (limit > 0)
                //Trim the queue down so there is room to add the next one
                while (Count >= Limit)
                    Dequeue();
            base.Enqueue(item);
        }
    }
}