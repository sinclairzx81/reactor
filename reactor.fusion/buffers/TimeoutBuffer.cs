using System;
using System.Collections.Generic;
using System.Text;

namespace Reactor.Fusion
{
    internal class TimeoutBufferItem
    {
        public PayloadSyn PayloadSyn     { get; set; }

        public DateTime   TimeStamp      { get; set; }

        public bool       Sent           { get; set; }

        public int        SentCount      { get; set; }

        public TimeoutBufferItem(PayloadSyn payload)
        {
            this.PayloadSyn = payload;

            this.TimeStamp = DateTime.Now;

            this.Sent      = false;

            this.SentCount = 0;
        }
    }

    internal class TimeoutBuffer
    {
        private object Lock { get; set; }

        public List<TimeoutBufferItem> List { get; set; }

        public TimeoutBuffer()
        {
            this.Lock = new object();

            this.List = new List<TimeoutBufferItem>();
        }

        public int Length
        {
            get
            {
                return this.List.Count;
            }
        }

        public int MaxRetries
        {
            get
            {
                lock(this.Lock)
                {
                    int count = 0;

                    for (int i = 0; i < this.List.Count; i++)
                    {
                        if (this.List[i].SentCount > count)
                        {
                            count = this.List[i].SentCount;
                        }
                    }

                    return count;
                }
            }
        }

        public int Timeout(double milliseconds)
        {
            lock(this.Lock)
            {
                int count = 0;

                for (int i = 0; i < this.List.Count; i++)
                {
                    var delta = DateTime.Now - this.List[i].TimeStamp;

                    if(delta.TotalMilliseconds > milliseconds) {

                        count += 1;

                        this.List[i].Sent = false;
                    }
                }

                return count;
            }
        }

        public void Write(PayloadSyn payload)
        {
            lock(this.Lock)
            {
                foreach(var item in this.List)
                {
                    if(item.PayloadSyn.SequenceNumber == payload.SequenceNumber)
                    {
                        return;
                    }
                }

                this.List.Add(new TimeoutBufferItem(payload));
            }
        }

        public List<PayloadSyn> Read(int numberOfPayloads)
        {
            lock(this.Lock)
            {
                List<PayloadSyn> result = new List<PayloadSyn>();

                foreach(var item in this.List) {

                    if(item.Sent == false) {

                        item.TimeStamp = DateTime.Now; // reset the timer on this item

                        item.Sent      = true;

                        item.SentCount = item.SentCount + 1;

                        result.Add(item.PayloadSyn);
                    }

                    if (result.Count == numberOfPayloads) {

                        return result;
                    }
                }

                return result;
            }
        }

        public void Acknowledge(uint acknowledgementNumber)
        {
            lock (this.Lock)
            {
                for (int i = 0; i < this.List.Count; i++)
                {
                    if (this.List[i].PayloadSyn.SequenceNumber == acknowledgementNumber)
                    {
                        this.List.RemoveRange(0, i);

                        return;
                    }
                }

                this.List.Clear();
            }
        }

    }
}
