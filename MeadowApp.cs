using Meadow;
using Meadow.Devices;
using Meadow.Foundation;
using Meadow.Foundation.Displays.TftSpi;
using Meadow.Foundation.Graphics;
using Meadow.Hardware;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MeadowClockGraphics
{
    public class MeadowApp : App<F7Micro, MeadowApp>
    {
        readonly GraphicsLibrary graphics;
        private int displayWidth = 239;
        private int displayHeight = 239;

        readonly Color WatchBackgroundColor = Color.White;

        public MeadowApp()
        {
            var config = new SpiClockConfiguration(6000, SpiClockConfiguration.Mode.Mode3);
            var st7789 = new St7789
            (
                device: Device,
                spiBus: Device.CreateSpiBus(Device.Pins.SCK, Device.Pins.MOSI, Device.Pins.MISO, config),
                chipSelectPin: null,
                dcPin: Device.Pins.D01,
                resetPin: Device.Pins.D00,
                width: 240, height: 240
            );

            graphics = new GraphicsLibrary(st7789);
            graphics.Rotation = RotationType._180Degrees;

            DrawTexts();

            Console.WriteLine("Init Wifi...");
            if (Device.WiFiAdapter.IsConnected)
            {
                Console.WriteLine("WiFi adapter already connected.");
            }
            else
            {
                Console.WriteLine("WiFi adapter not connected.");

                Device.WiFiAdapter.WiFiConnected += (s, e) => {
                    Console.WriteLine("WiFi adapter connected.");
                    gotTime.Set();
                };
            }

//            var res = ScanForAccessPoints();

            bool drawClock = true; 
            while (true)
            {
                Thread.Sleep(1000);

                Console.WriteLine($"Now = {DateTime.Now}");

                if (gotTime.WaitOne(0))
                {
                    if (drawClock)
                    {
                        DrawWatchFace();
                        drawClock = false;

                        // Display the current time zone name.  
                        Console.WriteLine("Local time zone Name : {0}\n", TimeZoneInfo.Local.DisplayName);
                        //apply base UTC offset  
                        localZone = TimeZoneInfo.CreateCustomTimeZone("NZ", new TimeSpan(0, 13, 0, 0), "NZ", "NZ");
                        Console.WriteLine("The {0} time zone is {1}:{2} {3} than Coordinated Universal Time.",
                            localZone.StandardName,
                            Math.Abs(localZone.BaseUtcOffset.Hours),
                            Math.Abs(localZone.BaseUtcOffset.Minutes),
                            (localZone.BaseUtcOffset >= TimeSpan.Zero) ? "later" : "earlier");
                        Console.WriteLine("Local Time Zone ID: {0}", localZone.Id);
                        Console.WriteLine("   Display Name is: {0}.", localZone.DisplayName);
                        Console.WriteLine("   Standard name is: {0}.", localZone.StandardName);
                        Console.WriteLine("   Daylight saving name is: {0}.", localZone.DaylightName);
                    }

                    DateTime dt = DateTime.Now;
                    Console.WriteLine($"Now = {dt}");
                    UpdateClock(dt);
                }
                else
                {
                    DrawShapes();
                }
            }
        }

        private static TimeZoneInfo localZone;

        private static ManualResetEvent gotTime = new ManualResetEvent(false);
        protected static async Task ScanForAccessPoints()
        {
            //Console.WriteLine("Getting list of access points.");
            //var networks = Device.WiFiAdapter?.Scan().ToList();
            //if (networks?.Count > 0)
            //{
            //    Console.WriteLine("|-------------------------------------------------------------|---------|");
            //    Console.WriteLine("|         Network Name             | RSSI |       BSSID       | Channel |");
            //    Console.WriteLine("|-------------------------------------------------------------|---------|");
            //    foreach (WifiNetwork accessPoint in networks)
            //    {
            //        Console.WriteLine($"| {accessPoint.Ssid,-32} | {accessPoint.SignalDbStrength,4} | {accessPoint.Bssid,17} |   {accessPoint.ChannelCenterFrequency,3}   |");
            //    }
            //}
            //else
            //{
            //    Console.WriteLine($"No access points detected.");
            //    return;
            //}

        }

        void DrawWatchFace()
        {
            graphics.Clear();
            int hour = 12;
            int xCenter = displayWidth / 2;
            int yCenter = displayHeight / 2;
            int x, y;
            graphics.DrawRectangle(0, 0, displayWidth, displayHeight, Color.White);
            graphics.DrawRectangle(5, 5, displayWidth - 10, displayHeight - 10, Color.White);
            graphics.CurrentFont = new Font12x20();
            graphics.DrawCircle(xCenter, yCenter, 100, WatchBackgroundColor, true);
            for (int i = 0; i < 60; i++)
            {
                x = (int)(xCenter + 80 * Math.Sin(i * Math.PI / 30));
                y = (int)(yCenter - 80 * Math.Cos(i * Math.PI / 30));

                if (i % 5 == 0)
                {
                    graphics.DrawText(
                        hour > 9 ? x - 10 : x - 5, y - 5, hour.ToString(), Color.Black);

                    if (hour == 12) hour = 1; else hour++;
                }
            }
            graphics.Show();
        }

        private int previousHour = -1;
        private int previousMinute =-1;
        private int previousSecond = -1;

        void UpdateClock(DateTime dt)
        {
            int xCenter = displayWidth / 2;
            int yCenter = displayHeight / 2;
            int x, y, xT, yT;

            int hour = dt.Hour;
            int minute = dt.Minute;
            int second = dt.Second;

            //remove previous hour

            if (previousHour >= 0)
            {
                x = (int)(xCenter + 43 * System.Math.Sin(previousHour * System.Math.PI / 6));
                y = (int)(yCenter - 43 * System.Math.Cos(previousHour * System.Math.PI / 6));
                xT = (int)(xCenter + 3 * System.Math.Sin((previousHour - 3) * System.Math.PI / 6));
                yT = (int)(yCenter - 3 * System.Math.Cos((previousHour - 3) * System.Math.PI / 6));
                graphics.DrawLine(xT, yT, x, y, WatchBackgroundColor);
                xT = (int)(xCenter + 3 * System.Math.Sin((previousHour + 3) * System.Math.PI / 6));
                yT = (int)(yCenter - 3 * System.Math.Cos((previousHour + 3) * System.Math.PI / 6));
                graphics.DrawLine(xT, yT, x, y, WatchBackgroundColor);
            }

            //remove previous minute
            if (previousMinute >= 0)
            {
                x = (int)(xCenter + 55 * System.Math.Sin(previousMinute * System.Math.PI / 30));
                y = (int)(yCenter - 55 * System.Math.Cos(previousMinute * System.Math.PI / 30));
                xT = (int)(xCenter + 3 * System.Math.Sin((previousMinute - 15) * System.Math.PI / 6));
                yT = (int)(yCenter - 3 * System.Math.Cos((previousMinute - 15) * System.Math.PI / 6));
                graphics.DrawLine(xT, yT, x, y, WatchBackgroundColor);
                xT = (int)(xCenter + 3 * System.Math.Sin((previousMinute + 15) * System.Math.PI / 6));
                yT = (int)(yCenter - 3 * System.Math.Cos((previousMinute + 15) * System.Math.PI / 6));
                graphics.DrawLine(xT, yT, x, y, WatchBackgroundColor);
            }

            //remove previous second
            if (previousSecond >= 0)
            {
                x = (int)(xCenter + 70 * System.Math.Sin(previousSecond * System.Math.PI / 30));
                y = (int)(yCenter - 70 * System.Math.Cos(previousSecond * System.Math.PI / 30));
                graphics.DrawLine(xCenter, yCenter, x, y, WatchBackgroundColor);
            }

            //current hour
            x = (int)(xCenter + 43 * System.Math.Sin(hour * System.Math.PI / 6));
            y = (int)(yCenter - 43 * System.Math.Cos(hour * System.Math.PI / 6));
            xT = (int)(xCenter + 3 * System.Math.Sin((hour - 3) * System.Math.PI / 6));
            yT = (int)(yCenter - 3 * System.Math.Cos((hour - 3) * System.Math.PI / 6));
            graphics.DrawLine(xT, yT, x, y, Color.Black);
            xT = (int)(xCenter + 3 * System.Math.Sin((hour + 3) * System.Math.PI / 6));
            yT = (int)(yCenter - 3 * System.Math.Cos((hour + 3) * System.Math.PI / 6));
            graphics.DrawLine(xT, yT, x, y, Color.Black);
            
            //current minute
            x = (int)(xCenter + 55 * System.Math.Sin(minute * System.Math.PI / 30));
            y = (int)(yCenter - 55 * System.Math.Cos(minute * System.Math.PI / 30));
            xT = (int)(xCenter + 3 * System.Math.Sin((minute - 15) * System.Math.PI / 6));
            yT = (int)(yCenter - 3 * System.Math.Cos((minute - 15) * System.Math.PI / 6));
            graphics.DrawLine(xT, yT, x, y, Color.Black);
            xT = (int)(xCenter + 3 * System.Math.Sin((minute + 15) * System.Math.PI / 6));
            yT = (int)(yCenter - 3 * System.Math.Cos((minute + 15) * System.Math.PI / 6));
            graphics.DrawLine(xT, yT, x, y, Color.Black);

            //current second
            x = (int)(xCenter + 70 * System.Math.Sin(second * System.Math.PI / 30));
            y = (int)(yCenter - 70 * System.Math.Cos(second * System.Math.PI / 30));
            graphics.DrawLine(xCenter, yCenter, x, y, Color.Red);

            graphics.Show();

            previousHour = hour;
            previousMinute = minute;
            previousSecond = second;
        }

        void DrawTexts()
        {
            graphics.Clear(true);
            
            int indent = 20;
            int spacing = 20;
            int y = 5;

            graphics.CurrentFont = new Font12x16();
            graphics.DrawText(indent, y, "Meadow F7 SPI ST7789!!");
            graphics.DrawText(indent, y += spacing, "Red", Color.Red);
            graphics.DrawText(indent, y += spacing, "Purple", Color.Purple);
            graphics.DrawText(indent, y += spacing, "BlueViolet", Color.BlueViolet);
            graphics.DrawText(indent, y += spacing, "Blue", Color.Blue);
            graphics.DrawText(indent, y += spacing, "Cyan", Color.Cyan);
            graphics.DrawText(indent, y += spacing, "LawnGreen", Color.LawnGreen);
            graphics.DrawText(indent, y += spacing, "GreenYellow", Color.GreenYellow);
            graphics.DrawText(indent, y += spacing, "Yellow", Color.Yellow);
            graphics.DrawText(indent, y += spacing, "Orange", Color.Orange);
            graphics.DrawText(indent, y += spacing, "Brown", Color.Brown);
            graphics.Show();

            Thread.Sleep(5000);
        }

        void DrawShapes()
        {
            Random rand = new Random();

            graphics.Clear(true);

            int radius = 10;
            int originX = displayWidth / 2;
            int originY = displayHeight / 2;
            for (int i = 1; i < 5; i++)
            {
                graphics.DrawCircle
                (
                    centerX: originX,
                    centerY: originY,
                    radius: radius,
                    color: Color.FromRgb(
                        rand.Next(255), rand.Next(255), rand.Next(255))
                );
                graphics.Show();
                radius += 30;
            }

            int sideLength = 30;
            for (int i = 1; i < 5; i++)
            {
                graphics.DrawRectangle
                (
                    x: (displayWidth - sideLength) / 2,
                    y: (displayHeight - sideLength) / 2,
                    width: sideLength,
                    height: sideLength,
                    color: Color.FromRgb(
                        rand.Next(255), rand.Next(255), rand.Next(255))
                );
                graphics.Show();
                sideLength += 60;
            }

            graphics.DrawLine(0, displayHeight / 2, displayWidth, displayHeight / 2,
                Color.FromRgb(rand.Next(255), rand.Next(255), rand.Next(255)));
            graphics.DrawLine(displayWidth / 2, 0, displayWidth / 2, displayHeight,
                Color.FromRgb(rand.Next(255), rand.Next(255), rand.Next(255)));
            graphics.DrawLine(0, 0, displayWidth, displayHeight,
                Color.FromRgb(rand.Next(255), rand.Next(255), rand.Next(255)));
            graphics.DrawLine(0, displayHeight, displayWidth, 0,
                Color.FromRgb(rand.Next(255), rand.Next(255), rand.Next(255)));
            graphics.Show();

            Thread.Sleep(5000);
        }
    }
}