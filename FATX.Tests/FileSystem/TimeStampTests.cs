using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;

namespace FATX.FileSystem.Tests
{
    [TestClass]
    public class TimeStampTests
    {
        [TestMethod]
        public void TestX360TimeStamp()
        {
            // value is already endian swapped..
            uint value = 0x44F75EE6;
            var timestamp = new X360TimeStamp(value);

            Assert.AreEqual(2014, timestamp.Year);
            Assert.AreEqual(7, timestamp.Month);
            Assert.AreEqual(23, timestamp.Day);
            Assert.AreEqual(11, timestamp.Hour);
            Assert.AreEqual(55, timestamp.Minute);
            Assert.AreEqual(12, timestamp.Second);
        }

        [TestMethod]
        public void TestXTimeStamp()
        {
            uint value = 0xC4FA079;
            var timestamp = new XTimeStamp(value);

            Assert.AreEqual(2006, timestamp.Year);
            Assert.AreEqual(2, timestamp.Month);
            Assert.AreEqual(15, timestamp.Day);
            Assert.AreEqual(20, timestamp.Hour);
            Assert.AreEqual(3, timestamp.Minute);
            Assert.AreEqual(50, timestamp.Second);
        }

        [TestMethod]
        public void TestX360TimeStampYear()
        {
            var timestamp = new X360TimeStamp();

            timestamp.Year = 2001;
            Assert.AreEqual(2001, timestamp.Year);
        }

        [TestMethod]
        public void TestX360TimeStampMonth()
        {
            var timestamp = new X360TimeStamp();

            timestamp.Month = 1;
            Assert.AreEqual(1, timestamp.Month);
        }

        [TestMethod]
        public void TestX360TimeStampDay()
        {
            var timestamp = new X360TimeStamp();

            timestamp.Day = 1;
            Assert.AreEqual(1, timestamp.Day);
        }

        [TestMethod]
        public void TestX360TimeStampHour()
        {
            var timestamp = new X360TimeStamp();

            timestamp.Hour = 1;
            Assert.AreEqual(1, timestamp.Hour);
        }

        [TestMethod]
        public void TestX360TimeStampMinute()
        {
            var timestamp = new X360TimeStamp();

            timestamp.Minute = 1;
            Assert.AreEqual(1, timestamp.Minute);
        }

        [TestMethod]
        public void TestX360TimeStampSeconds()
        {
            var timestamp = new X360TimeStamp();

            // It loses precision as it only stores half seconds internally.
            timestamp.Second = 1;
            Assert.AreEqual(0, timestamp.Second);

            timestamp.Second = 2;
            Assert.AreEqual(2, timestamp.Second);

            timestamp.Second = 60;
            Assert.AreEqual(60, timestamp.Second);
        }

        [TestMethod]
        public void TestXTimeStampYear()
        {
            var timestamp = new XTimeStamp();

            timestamp.Year = 2001;
            Assert.AreEqual(2001, timestamp.Year);
        }

        [TestMethod]
        public void TestXTimeStampMonth()
        {
            var timestamp = new XTimeStamp();
            
            timestamp.Month = 1;
            Assert.AreEqual(1, timestamp.Month);
        }

        [TestMethod]
        public void TestXTimeStampDay()
        {
            var timestamp = new XTimeStamp();

            timestamp.Day = 1;
            Assert.AreEqual(1, timestamp.Day);
        }

        [TestMethod]
        public void TestXTimeStampHour()
        {
            var timestamp = new XTimeStamp();

            timestamp.Hour = 1;
            Assert.AreEqual(1, timestamp.Hour);
        }

        [TestMethod]
        public void TestXTimeStampMinute()
        {
            var timestamp = new XTimeStamp();

            timestamp.Minute = 1;
            Assert.AreEqual(1, timestamp.Minute);
        }

        [TestMethod]
        public void TestXTimeStampSecond()
        {
            var timestamp = new XTimeStamp();

            // It loses precision as it only stores half seconds internally.
            timestamp.Second = 1;
            Assert.AreEqual(0, timestamp.Second);

            timestamp.Second = 2;
            Assert.AreEqual(2, timestamp.Second);

            timestamp.Second = 60;
            Assert.AreEqual(60, timestamp.Second);
        }
    }
}