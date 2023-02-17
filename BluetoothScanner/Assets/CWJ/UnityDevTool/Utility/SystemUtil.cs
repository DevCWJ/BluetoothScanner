using System;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Text;
using UnityEngine.Rendering;

namespace CWJ
{
    public static class SystemUtil
    {
        /// <summary>
        /// check server build (headless) mode
        /// </summary>
        public static bool IsServerBuild => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

        public static string SecondToDigitalMinute(float value)
        {
            int hour = 0;
            int minute = 0;
            int second = 0;

            if (value > 60 * 60)
            {
                hour = UnityEngine.Mathf.FloorToInt(value / 60.0f / 60.0f);
            }

            if (value > 60)
            {
                minute = UnityEngine.Mathf.FloorToInt(value / 60.0f);
            }

            second = UnityEngine.Mathf.FloorToInt(value % 60.0f);

            return (hour == 0 ? "" : (hour < 10 ? "0" : "") + hour.ToString() + ":") + (minute < 10 ? "0" : "") + minute.ToString() + ":" + (second < 10 ? "0" : "") + second.ToString();
        }
        public static string TimeSpanToString(this TimeSpan timeSpan, bool isVisibleMilli = false)
        {
            return $"{(int)timeSpan.TotalHours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}" + (isVisibleMilli ? $":{timeSpan.Milliseconds:000}" : "");
        }

        public static string HoursToTime(this double hours, bool isVisibleMilli = false)
        {
            return TimeSpanToString(TimeSpan.FromHours(hours), isVisibleMilli);
        }

        public static string MinutesToTime(this double minutes, bool isVisibleMilli = false)
        {
            return TimeSpanToString(TimeSpan.FromMinutes(minutes), isVisibleMilli);
        }

        public static string SecondsToTime(this double seconds, bool isVisibleMilli = false)
        {
            return TimeSpanToString(TimeSpan.FromSeconds(seconds), isVisibleMilli);
        }

        public static string MilliSecondsToTime(this double milliSeconds, bool isVisibleMilli = false)
        {
            return TimeSpanToString(TimeSpan.FromMilliseconds(milliSeconds), isVisibleMilli);
        }

        //public static void SendMessageWithDelay(this Action action, float delay)
        //{
        //    Timer timer = new Timer(delay);

        //    timer.Elapsed +=(a,b)=>
        //    {
        //        action.Invoke();
        //        timer.Stop();
        //        timer.Dispose();
        //    };

        //    timer.Start();
        //}
    }
}