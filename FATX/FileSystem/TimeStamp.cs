using System;

namespace FATX.FileSystem
{
    public class X360TimeStamp : TimeStamp
    {
        public X360TimeStamp(uint time) : base(time) 
        {

        }

        public override int Year => base.Year + 1980;
    }

    public class XTimeStamp : TimeStamp
    {
        public XTimeStamp(uint time) : base(time)
        {

        }

        public override int Year => base.Year + 2000;
    }

    public class TimeStamp
    {
        uint _time;
        DateTime? _dataTime;

        public TimeStamp(uint time)
        {
            this._time = time;
        }

        public virtual int Year => (int)((this._time & 0xFE000000) >> 25);
        public virtual int Month => (int)((this._time & 0x1E00000) >> 21);
        public virtual int Day => (int)((this._time & 0x1F0000) >> 16);
        public virtual int Hour => (int)((this._time & 0xF800) >> 11);
        public virtual int Minute => (int)((this._time & 0x7E0) >> 5);
        public virtual int Second => (int)((this._time & 0x1F) * 2);

        public uint AsInteger()
        {
            return _time;
        }

        public DateTime AsDateTime()
        {
            if (!this._dataTime.HasValue)
            {
                try
                {
                    _dataTime = new DateTime(
                        this.Year, this.Month, this.Day,
                        this.Hour, this.Minute, this.Second
                    );
                }
                catch (Exception)
                {
                    try
                    {
                        _dataTime = new DateTime(
                            (int)(((_time & 0xffff) & 0x7F) + 2000),
                            (int)(((_time & 0xffff) >> 7) & 0xF),
                            (int)((_time & 0xffff) >> 0xB),
                            (int)((_time >> 16) & 0x1f),
                            (int)(((_time >> 16) >> 5) & 0x3F),
                            (int)(((_time >> 16) >> 10) & 0xfffe)
                        );
                    }
                    catch (Exception)
                    {
                        _dataTime = DateTime.MinValue;
                    }
                }
            }

            return _dataTime.Value;
        }
    }
}