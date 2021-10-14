using System;
using System.Runtime.CompilerServices;

namespace FATX.FileSystem
{
    /// <summary>
    /// Represents an encoded timestamp for the xbox 360.
    /// 
    /// Bit Format:
    ///     Year : 7 bits
    ///     Month : 4 bits
    ///     Day : 5 bits
    ///     Hour : 5 bits
    ///     Minute : 6 bits
    ///     DoubleSeconds : 5 bits
    /// </summary>
    public class X360TimeStamp : TimeStamp
    {
        public X360TimeStamp() : base() { }
        public X360TimeStamp(uint time) : base(time) { }

        /// <summary>
        /// The year is relative to 1980 on the xbox 360.
        /// </summary>
        public override int Year
        {
            get => base.Year + 1980;
            set => base.Year = value - 1980;
        }
    }

    /// <summary>
    /// Represents an encoded timestamp for the original xbox.
    /// 
    /// Bit Format:
    ///     Year : 7 bits
    ///     Month : 4 bits
    ///     Day : 5 bits
    ///     Hour : 5 bits
    ///     Minute : 6 bits
    ///     DoubleSeconds : 5 bits
    /// </summary>
    public class XTimeStamp : TimeStamp
    {
        public XTimeStamp() : base() { }
        public XTimeStamp(uint time) : base(time) { }

        /// <summary>
        /// The year is relative to 2000 on the original xbox.
        /// </summary>
        public override int Year
        {
            get => base.Year + 2000;
            set => base.Year = value - 2000;
        }
    }

    /// <summary>
    /// Base class for the original xbox and xbox 360 timestamps.
    /// </summary>
    public abstract class TimeStamp
    {
        uint _time;
        DateTime? _dateTime;

        public TimeStamp() : this(0) { }
        public TimeStamp(uint time)
        {
            _time = time;
        }

        /// <summary>
        /// The year represented by 7 bits.
        /// </summary>
        public virtual int Year
        {
            get => (int)((_time & 0xFE000000) >> 25);
            set => _time = ((uint)(_time & ~(0xFE000000)) | (((uint)value & 0x7F) << 25));
        }

        /// <summary>
        /// The month represented by 4 bits.
        /// </summary>
        public virtual int Month
        {
            get => (int)((_time & 0x1E00000) >> 21);
            set => _time = ((uint)(_time & ~(0x1E00000)) | (((uint)value & 0xF) << 21));
        }

        /// <summary>
        /// The day represented by 5 bits.
        /// </summary>
        public virtual int Day
        {
            get => (int)((_time & 0x1F0000) >> 16);
            set => _time = ((uint)(_time & ~(0x1F0000)) | (((uint)value & 0x1F) << 16));
        }

        /// <summary>
        /// The hour represented by 5 bits.
        /// </summary>
        public virtual int Hour
        {
            get => (int)((_time & 0xF800) >> 11);
            set => _time = ((uint)(_time & ~(0xF800)) | (((uint)value & 0x1F) << 11));
        }

        /// <summary>
        /// The minute represented by 6 bits.
        /// </summary>
        public virtual int Minute
        {
            get => (int)((_time & 0x7E0) >> 5);
            set => _time = ((uint)(_time & ~(0x7E0)) | (((uint)value & 0x3F) << 5));
        }

        /// <summary>
        /// The second represented by 5 bits. The internal value represents double seconds.
        /// </summary>
        public virtual int Second
        {
            get => (int)((_time & 0x1F) * 2);
            set => _time = ((uint)(_time & ~(0x1F)) | ((uint)(value / 2) & 0x1F));
        }

        /// <summary>
        /// Gets the raw timestamp.
        /// </summary>
        /// <returns>The raw timestamp,.</returns>
        public uint AsInteger()
        {
            return _time;
        }

        /// <summary>
        /// Gets the timestamp as a DateTime object.
        /// </summary>
        /// <returns>The timestamp as a DateTime object.</returns>
        public DateTime AsDateTime()
        {
            return _dateTime.HasValue ? _dateTime.Value : TryConvertToDateTime();
        }

        /// <summary>
        /// Tries to parse the timestamp to a DateTime object. If it fails, then we try to parse it differently.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DateTime TryConvertToDateTime()
        {
            try
            {
                // Try and create a DateTime from what we have.
                _dateTime = new DateTime(
                    Year, Month, Day,
                    Hour, Minute, Second
                );
            }
            catch (Exception)
            {
                // Try parsing the timestamp in a different manner.
                try
                {
                    // This is the same as the regular format, just the fields are swapped.
                    // Bit Format:
                    //     DoubleSeconds : 5 bits
                    //     Minute : 6 bits
                    //     Hour : 5 bits
                    //     Day : 5 bits
                    //     Month : 4 bits
                    //     Year : 7 bits
                    _dateTime = new DateTime(
                        (int)(((_time & 0xffff) & 0x7F) + 2000),    // Year
                        (int)(((_time & 0xffff) >> 7) & 0xF),       // Month
                        (int)((_time & 0xffff) >> 0xB),             // Day
                        (int)((_time >> 16) & 0x1f),                // Hour
                        (int)(((_time >> 16) >> 5) & 0x3F),         // Minute
                        (int)(((_time >> 16) >> 10) & 0xfffe)       // Second
                    );
                }
                catch (Exception)
                {
                    // Just default to minimum value.
                    // Hopefully the user can infer that the timestamp was an unknown/invalid format from that.
                    _dateTime = DateTime.MinValue;
                }
            }

            return _dateTime.Value;
        }
    }
}