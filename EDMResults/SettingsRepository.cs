using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Text;

namespace EDMResults.Core
{
    public sealed class RecordedAttemptEntry
    {
        public int SequenceNumber { get; set; }
        public string TimestampText { get; set; }
        public string DisciplineLabel { get; set; }
        public string OfficialMarkText { get; set; }
        public double NumericMeters { get; set; }
        public bool IsFoulOrNull { get; set; }
        public string RawLedgerLine { get; set; }

        public static RecordedAttemptEntry ParseLedgerLine(int sequence, string rawLine, string fallbackDiscipline)
        {
            string trimmed = (rawLine ?? string.Empty).Trim();
            var entry = new RecordedAttemptEntry
            {
                SequenceNumber = sequence,
                RawLedgerLine = trimmed,
                DisciplineLabel = fallbackDiscipline ?? I18n.T("Ledger.DefaultDiscipline"),
                TimestampText = "--:--",
                OfficialMarkText = trimmed,
                NumericMeters = -1.0,
                IsFoulOrNull = false
            };

            if (trimmed.Length >= 5 && trimmed[2] == ':')
            {
                entry.TimestampText = trimmed.Substring(0, 5);
                entry.OfficialMarkText = trimmed.Substring(5).Trim();
            }

            if (entry.OfficialMarkText.IndexOf("NULL", StringComparison.OrdinalIgnoreCase) >= 0 ||
                entry.OfficialMarkText.IndexOf("FOUL", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                entry.IsFoulOrNull = true;
                entry.OfficialMarkText = I18n.T("Mark.Foul");
            }
            else
            {
                string numericSlice = entry.OfficialMarkText
                    .Replace("m", string.Empty)
                    .Replace(" ", string.Empty)
                    .Replace(',', '.');
                double parsed;
                if (double.TryParse(numericSlice, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                {
                    entry.NumericMeters = parsed;
                }
            }

            return entry;
        }
    }

    public sealed class WorkspaceStateSnapshot
    {
        public string LanguageCode { get; set; }
        public int DisciplineCode { get; set; }
        public bool LockCalibrationControls { get; set; }
        public int CircleRadiusOffsetMm { get; set; }
        public PolarReading CalibratedPoint1 { get; set; }
        public PolarReading CalibratedPoint2 { get; set; }
        public SerialEndpointProfile SerialEndpoint { get; set; }

        public WorkspaceStateSnapshot()
        {
            LanguageCode = "it";
            DisciplineCode = 0;
            LockCalibrationControls = false;
            CircleRadiusOffsetMm = 1068;
            SerialEndpoint = new SerialEndpointProfile();
        }
    }

    public static class WorkspacePersistenceService
    {
        private const string ConfigFileName = "Config.txt";
        private const string LedgerFileName = "Misure.txt";

        private static string ResolveLocalPath(string fileName)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        }

        public static WorkspaceStateSnapshot LoadWorkspaceState()
        {
            var snapshot = new WorkspaceStateSnapshot();
            string path = ResolveLocalPath(ConfigFileName);
            if (!File.Exists(path)) return snapshot;

            try
            {
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    int sep = line.IndexOf('=');
                    if (sep <= 0) continue;
                    string key = line.Substring(0, sep).Trim();
                    string val = line.Substring(sep + 1).Trim();
                    map[key] = val;
                }

                string langCode;
                if ((map.TryGetValue("Language", out langCode) || map.TryGetValue("Lingua", out langCode)) && !string.IsNullOrWhiteSpace(langCode))
                {
                    snapshot.LanguageCode = langCode.Trim().ToLowerInvariant();
                }

                snapshot.DisciplineCode = ClampRange(ReadIntValue(map, "Tipo", 0), 0, DisciplineSpec.Catalog.Length - 1);
                snapshot.LockCalibrationControls = ReadBoolValue(map, "Lock", false);

                int defaultOffset = DisciplineSpec.Catalog[snapshot.DisciplineCode].StandardRadiusMm;
                snapshot.CircleRadiusOffsetMm = ClampRange(ReadIntValue(map, "OffsetZero", defaultOffset), -100000, 100000);

                if (ReadBoolValue(map, "Punto1", false))
                {
                    double h1 = ReadDoubleValue(map, "Punto1.O", 0.0);
                    double v1 = ReadDoubleValue(map, "Punto1.V", 100.0);
                    double d1 = ReadDoubleValue(map, "Punto1.D", 0.0);
                    if (d1 > 0.0)
                    {
                        snapshot.CalibratedPoint1 = PolarReading.FromGon(h1, v1, d1, I18n.T("Profile.Saved"));
                    }
                }

                if (ReadBoolValue(map, "Punto2", false))
                {
                    double h2 = ReadDoubleValue(map, "Punto2.O", 0.0);
                    double v2 = ReadDoubleValue(map, "Punto2.V", 100.0);
                    double d2 = ReadDoubleValue(map, "Punto2.D", 0.0);
                    if (d2 > 0.0)
                    {
                        snapshot.CalibratedPoint2 = PolarReading.FromGon(h2, v2, d2, I18n.T("Profile.Saved"));
                    }
                }

                string comPort;
                if (map.TryGetValue("Seriale.Port", out comPort) && !string.IsNullOrWhiteSpace(comPort))
                {
                    snapshot.SerialEndpoint.PortName = comPort;
                }
                snapshot.SerialEndpoint.BaudRate = ReadIntValue(map, "Seriale.Speed", 9600);
                snapshot.SerialEndpoint.DataBits = ReadIntValue(map, "Seriale.Bits", 8);

                int parityIdx = ReadIntValue(map, "Seriale.Parity", 0);
                if (Enum.IsDefined(typeof(Parity), parityIdx))
                {
                    snapshot.SerialEndpoint.ParityMode = (Parity)parityIdx;
                }

                int stopIdx = ReadIntValue(map, "Seriale.StopBits", 1);
                if (Enum.IsDefined(typeof(StopBits), stopIdx) && (StopBits)stopIdx != StopBits.None)
                {
                    snapshot.SerialEndpoint.StopBitsMode = (StopBits)stopIdx;
                }

                int protoIdx = ReadIntValue(map, "Seriale.Protocol", 0);
                if (Enum.IsDefined(typeof(InstrumentProtocolPreference), protoIdx))
                {
                    snapshot.SerialEndpoint.ProtocolPreference = (InstrumentProtocolPreference)protoIdx;
                }
            }
            catch
            {
            }

            return snapshot;
        }

        public static void SaveWorkspaceState(WorkspaceStateSnapshot snapshot)
        {
            if (snapshot == null) return;
            try
            {
                var lines = new List<string>(22)
                {
                    "Language=" + (string.IsNullOrWhiteSpace(snapshot.LanguageCode) ? I18n.CurrentLanguageCode : snapshot.LanguageCode),
                    "Tipo=" + snapshot.DisciplineCode.ToString(CultureInfo.InvariantCulture),
                    "Lock=" + (snapshot.LockCalibrationControls ? "true" : "false"),
                    "OffsetZero=" + snapshot.CircleRadiusOffsetMm.ToString(CultureInfo.InvariantCulture),
                    "Punto1=" + (snapshot.CalibratedPoint1 != null ? "true" : "false")
                };

                if (snapshot.CalibratedPoint1 != null)
                {
                    lines.Add("Punto1.O=" + snapshot.CalibratedPoint1.HorizontalGon.ToString("R", CultureInfo.InvariantCulture));
                    lines.Add("Punto1.V=" + snapshot.CalibratedPoint1.VerticalGon.ToString("R", CultureInfo.InvariantCulture));
                    lines.Add("Punto1.D=" + snapshot.CalibratedPoint1.SlopeDistanceM.ToString("R", CultureInfo.InvariantCulture));
                }

                lines.Add("Punto2=" + (snapshot.CalibratedPoint2 != null ? "true" : "false"));
                if (snapshot.CalibratedPoint2 != null)
                {
                    lines.Add("Punto2.O=" + snapshot.CalibratedPoint2.HorizontalGon.ToString("R", CultureInfo.InvariantCulture));
                    lines.Add("Punto2.V=" + snapshot.CalibratedPoint2.VerticalGon.ToString("R", CultureInfo.InvariantCulture));
                    lines.Add("Punto2.D=" + snapshot.CalibratedPoint2.SlopeDistanceM.ToString("R", CultureInfo.InvariantCulture));
                }

                SerialEndpointProfile ep = snapshot.SerialEndpoint ?? new SerialEndpointProfile();
                lines.Add("Seriale.Port=" + (ep.PortName ?? "COM1"));
                lines.Add("Seriale.Speed=" + ep.BaudRate.ToString(CultureInfo.InvariantCulture));
                lines.Add("Seriale.Bits=" + ep.DataBits.ToString(CultureInfo.InvariantCulture));
                lines.Add("Seriale.Parity=" + ((int)ep.ParityMode).ToString(CultureInfo.InvariantCulture));
                lines.Add("Seriale.StopBits=" + ((int)ep.StopBitsMode).ToString(CultureInfo.InvariantCulture));
                lines.Add("Seriale.Protocol=" + ((int)ep.ProtocolPreference).ToString(CultureInfo.InvariantCulture));

                File.WriteAllLines(ResolveLocalPath(ConfigFileName), lines.ToArray(), Encoding.UTF8);
            }
            catch
            {
            }
        }

        public static List<RecordedAttemptEntry> ReadSavedLedger(string currentDisciplineName)
        {
            var results = new List<RecordedAttemptEntry>();
            string path = ResolveLocalPath(LedgerFileName);
            if (!File.Exists(path)) return results;

            try
            {
                string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                int seq = 1;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;
                    results.Add(RecordedAttemptEntry.ParseLedgerLine(seq++, lines[i], currentDisciplineName));
                }
            }
            catch
            {
            }

            return results;
        }

        public static RecordedAttemptEntry AppendLedgerRecord(string formattedMark, string disciplineName)
        {
            string stamp = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);
            string ledgerLine = stamp + "  " + formattedMark;
            try
            {
                File.AppendAllText(ResolveLocalPath(LedgerFileName), ledgerLine + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
            }

            return RecordedAttemptEntry.ParseLedgerLine(0, ledgerLine, disciplineName);
        }

        public static void ClearSavedLedger()
        {
            try
            {
                string path = ResolveLocalPath(LedgerFileName);
                if (File.Exists(path))
                {
                    File.WriteAllText(path, string.Empty, Encoding.UTF8);
                }
            }
            catch
            {
            }
        }

        private static int ReadIntValue(Dictionary<string, string> map, string key, int fallback)
        {
            string raw;
            int parsed;
            if (map.TryGetValue(key, out raw) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }
            return fallback;
        }

        private static bool ReadBoolValue(Dictionary<string, string> map, string key, bool fallback)
        {
            string raw;
            bool parsed;
            if (map.TryGetValue(key, out raw) && bool.TryParse(raw, out parsed))
            {
                return parsed;
            }
            return fallback;
        }

        private static double ReadDoubleValue(Dictionary<string, string> map, string key, double fallback)
        {
            string raw;
            if (!map.TryGetValue(key, out raw) || string.IsNullOrWhiteSpace(raw)) return fallback;

            double parsed;
            if (double.TryParse(raw.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }
            return fallback;
        }

        private static int ClampRange(int val, int min, int max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }
    }
}
