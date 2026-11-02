using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;

namespace EDMResults.Core
{
    public sealed class AppProfile
    {
        public int DisciplineIndex { get; set; }
        public bool OffsetLocked { get; set; }
        public int OffsetMm { get; set; }
        public SurveyPoint Point1 { get; set; }
        public SurveyPoint Point2 { get; set; }
        public SerialConfiguration Serial { get; set; }

        public AppProfile()
        {
            DisciplineIndex = 0;
            OffsetLocked = true;
            OffsetMm = DisciplineInfo.All[0].DefaultOffsetMm;
            Serial = new SerialConfiguration();
        }
    }

    public static class SettingsRepository
    {
        public static AppProfile LoadConfig(string filePath)
        {
            AppProfile profile = new AppProfile();
            if (!File.Exists(filePath))
            {
                return profile;
            }

            Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string rawLine in File.ReadAllLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(rawLine)) continue;
                int eq = rawLine.IndexOf('=');
                if (eq <= 0) continue;
                string key = rawLine.Substring(0, eq).Trim().ToUpperInvariant();
                string val = rawLine.Substring(eq + 1).Trim();
                map[key] = val;
            }

            int spec;
            if (map.ContainsKey("SPEC") && int.TryParse(map["SPEC"], out spec) && spec >= 0 && spec < DisciplineInfo.All.Length)
            {
                profile.DisciplineIndex = spec;
            }

            int lockFlag;
            if (map.ContainsKey("OFFSETLOCK") && int.TryParse(map["OFFSETLOCK"], out lockFlag))
            {
                profile.OffsetLocked = (lockFlag != 0);
            }

            int offset;
            if (map.ContainsKey("OFFSET") && int.TryParse(map["OFFSET"], out offset))
            {
                profile.OffsetMm = offset;
            }
            else
            {
                profile.OffsetMm = DisciplineInfo.All[profile.DisciplineIndex].DefaultOffsetMm;
            }

            if (map.ContainsKey("P1HZ") && map.ContainsKey("P1V") && map.ContainsKey("P1DIST"))
            {
                double hz, v, dist;
                if (TryParseDouble(map["P1HZ"], out hz) && TryParseDouble(map["P1V"], out v) && TryParseDouble(map["P1DIST"], out dist))
                {
                    profile.Point1 = new SurveyPoint(hz, v, dist);
                }
            }

            if (map.ContainsKey("P2HZ") && map.ContainsKey("P2V") && map.ContainsKey("P2DIST"))
            {
                double hz, v, dist;
                if (TryParseDouble(map["P2HZ"], out hz) && TryParseDouble(map["P2V"], out v) && TryParseDouble(map["P2DIST"], out dist))
                {
                    profile.Point2 = new SurveyPoint(hz, v, dist);
                }
            }

            int comNum;
            if (map.ContainsKey("COM") && int.TryParse(map["COM"], out comNum) && comNum >= 1)
            {
                profile.Serial.PortName = "COM" + comNum;
            }

            int sendPc;
            if (map.ContainsKey("SENDPC") && int.TryParse(map["SENDPC"], out sendPc))
            {
                profile.Serial.ForwardToScoreboard = (sendPc != 0);
            }

            int bps;
            if (map.ContainsKey("BPS") && int.TryParse(map["BPS"], out bps))
            {
                profile.Serial.BaudRate = bps;
            }

            int dataBit;
            if (map.ContainsKey("DATABIT") && int.TryParse(map["DATABIT"], out dataBit))
            {
                profile.Serial.DataBits = (dataBit == 7) ? 7 : 8;
            }

            if (map.ContainsKey("PARITY"))
            {
                string p = map["PARITY"].ToUpperInvariant();
                if (p == "E") profile.Serial.Parity = Parity.Even;
                else if (p == "O") profile.Serial.Parity = Parity.Odd;
                else profile.Serial.Parity = Parity.None;
            }

            int stopBit;
            if (map.ContainsKey("STOPBIT") && int.TryParse(map["STOPBIT"], out stopBit))
            {
                profile.Serial.StopBits = (stopBit == 2) ? StopBits.Two : StopBits.One;
            }

            return profile;
        }

        public static void SaveConfig(string filePath, AppProfile profile)
        {
            List<string> lines = new List<string>();
            lines.Add("SPEC=" + profile.DisciplineIndex);
            lines.Add("OFFSETLOCK=" + (profile.OffsetLocked ? "1" : "0"));
            lines.Add("OFFSET=" + profile.OffsetMm);

            if (profile.Point1 != null)
            {
                lines.Add("P1HZ=" + profile.Point1.HorizontalGon);
                lines.Add("P1V=" + profile.Point1.VerticalGon);
                lines.Add("P1DIST=" + profile.Point1.SlopeDistanceMm);
            }

            if (profile.Point2 != null)
            {
                lines.Add("P2HZ=" + profile.Point2.HorizontalGon);
                lines.Add("P2V=" + profile.Point2.VerticalGon);
                lines.Add("P2DIST=" + profile.Point2.SlopeDistanceMm);
            }

            int portNumber = 1;
            if (!string.IsNullOrEmpty(profile.Serial.PortName) && profile.Serial.PortName.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
            {
                int.TryParse(profile.Serial.PortName.Substring(3), out portNumber);
                if (portNumber < 1) portNumber = 1;
            }

            lines.Add("COM=" + portNumber);
            lines.Add("SENDPC=" + (profile.Serial.ForwardToScoreboard ? "1" : "0"));
            lines.Add("BPS=" + profile.Serial.BaudRate);
            lines.Add("DATABIT=" + profile.Serial.DataBits);

            string parityCode = "N";
            if (profile.Serial.Parity == Parity.Even) parityCode = "E";
            else if (profile.Serial.Parity == Parity.Odd) parityCode = "O";
            lines.Add("PARITY=" + parityCode);

            lines.Add("STOPBIT=" + (profile.Serial.StopBits == StopBits.Two ? "2" : "1"));

            File.WriteAllLines(filePath, lines.ToArray());
        }

        private static bool TryParseDouble(string raw, out double result)
        {
            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out result))
            {
                return true;
            }
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }
    }
}

