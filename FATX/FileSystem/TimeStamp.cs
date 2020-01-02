using System;
using System.Collections.Generic;
using System.Text;

namespace FATX
{
    public class X360TimeStamp : TimeStamp
    {
        public X360TimeStamp(uint time)
            : base(time)
        {

        }
        public override int Year => base.Year + 1980;
    }

    public class XTimeStamp : TimeStamp
    {
        public XTimeStamp(uint time)
            : base(time)
        {

        }
        public override int Year => base.Year + 2000;
    }

    public class TimeStamp
    {
        private uint _Time;

        public TimeStamp(uint time)
        {
            this._Time = time;
        }
        public virtual int Year
        {
            get { return (int)((this._Time & 0xFE000000) >> 25); }
        }

        public virtual int Month
        {
            get { return (int)((this._Time & 0x1E00000) >> 21); }
        }

        public virtual int Day
        {
            get { return (int)((this._Time & 0x1F0000) >> 16); }
        }

        public virtual int Hour
        {
            get { return (int)((this._Time & 0xF800) >> 11); }
        }

        public virtual int Minute
        {
            get { return (int)((this._Time & 0x7E0) >> 5); }
        }

        public virtual int Second
        {
            get { return (int)((this._Time & 0x1F) * 2); }
        }
    }
}
