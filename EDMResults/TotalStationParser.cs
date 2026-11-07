using System;
using System.Globalization;

namespace EDMResults.Core
{
    public enum InstrumentProtocolPreference
    {
        AutoRecognize = 0,
        MatoDms21 = 1,
        SokkiaSetGon23 = 2,
        LeicaStonexGsi = 3,
        NikonAscii = 4
    }

    public static class TelemetryFrameClassifier
    {
        public static PolarReading TryParsePacket(
            string rawPacket,
            InstrumentProtocolPreference preference = InstrumentProtocolPreference.AutoRecognize)
        {
            if (string.IsNullOrEmpty(rawPacket)) return null;

            string cleaned = SanitizeControlCharacters(rawPacket);
            if (cleaned.Length < 12) return null;

            if (preference == InstrumentProtocolPreference.MatoDms21)
            {
                return TryParseMato21Block(cleaned) ?? TryParseAnyProtocol(cleaned);
            }
            if (preference == InstrumentProtocolPreference.SokkiaSetGon23)
            {
                return TryParseSet23Block(cleaned) ?? TryParseAnyProtocol(cleaned);
            }
            if (preference == InstrumentProtocolPreference.LeicaStonexGsi)
            {
                return TryParseGsiWordPacket(cleaned) ?? TryParseAnyProtocol(cleaned);
            }
            if (preference == InstrumentProtocolPreference.NikonAscii)
            {
                return TryParseKeyValuePacket(cleaned) ?? TryParseAnyProtocol(cleaned);
            }

            return TryParseAnyProtocol(cleaned);
        }

        private static PolarReading TryParseAnyProtocol(string cleaned)
        {
            // Structural pattern classification based on packet grammar (not first-character switch):
            // 1. Key-value labeled telemetry (contains "HA", "VA", or "SD" markers)
            if (ContainsLabeledTokens(cleaned))
            {
                PolarReading kv = TryParseKeyValuePacket(cleaned);
                if (kv != null) return kv;
            }

            // 2. Space-delimited GSI word blocks (words starting with 21.., 22.., 31..)
            if (cleaned.IndexOf(' ') >= 0 && (cleaned.Contains("21.") || cleaned.Contains("22.") || cleaned.Contains("31.")))
            {
                PolarReading gsi = TryParseGsiWordPacket(cleaned);
                if (gsi != null) return gsi;
            }

            // 3. Contiguous numeric block (MATO 21-digit DMS or Sokkia/SET 23-digit Gon)
            string contiguousDigits = ExtractLeadingContiguousDigits(cleaned);
            if (contiguousDigits.Length >= 23)
            {
                PolarReading setReading = TryParseSet23Block(contiguousDigits);
                if (setReading != null) return setReading;
            }
            if (contiguousDigits.Length == 21 || contiguousDigits.Length == 22)
            {
                PolarReading matoReading = TryParseMato21Block(contiguousDigits);
                if (matoReading != null) return matoReading;
            }

            // Fallback attempts across remaining grammars
            return TryParseGsiWordPacket(cleaned)
                ?? TryParseSet23Block(cleaned)
                ?? TryParseMato21Block(cleaned);
        }

        private static string SanitizeControlCharacters(string raw)
        {
            char[] buffer = new char[raw.Length];
            int count = 0;
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (c >= 32 && c <= 126)
                {
                    buffer[count++] = c;
                }
                else if (c == '\t')
                {
                    buffer[count++] = ' ';
                }
            }
            return new string(buffer, 0, count).Trim();
        }

        private static string ExtractLeadingContiguousDigits(string text)
        {
            int start = -1;
            int length = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c >= '0' && c <= '9')
                {
                    if (start < 0) start = i;
                    length++;
                }
                else if (start >= 0)
                {
                    if (length >= 21)
                    {
                        return text.Substring(start, length);
                    }
                    start = -1;
                    length = 0;
                }
            }
            return (start >= 0 && length >= 21) ? text.Substring(start, length) : string.Empty;
        }

        /// <summary>
        /// Decodes MATO MTS-602R+ / 21-digit packed frame:
        /// [0..6] (7 digits) = Slope Distance in mm
        /// [7..13] (7 digits) = Vertical Angle in DDDMMSS (Sexagesimal Degrees/Minutes/Seconds)
        /// [14..20] (7 digits) = Horizontal Angle in DDDMMSS (Sexagesimal Degrees/Minutes/Seconds)
        /// </summary>
        private static PolarReading TryParseMato21Block(string input)
        {
            string digits = ExtractLeadingContiguousDigits(input);
            if (digits.Length < 21) return null;

            long distMm;
            if (!long.TryParse(digits.Substring(0, 7), NumberStyles.None, CultureInfo.InvariantCulture, out distMm))
            {
                return null;
            }

            double vertDeg;
            double horizDeg;
            if (TryDecodePackedDms7(digits.Substring(7, 7), out vertDeg) &&
                TryDecodePackedDms7(digits.Substring(14, 7), out horizDeg))
            {
                return PolarReading.FromSexagesimalDegrees(
                    horizDeg,
                    vertDeg,
                    distMm * 0.001,
                    "MATO MTS-602R+ (21d DMS)",
                    digits.Substring(0, 21));
            }

            // Fallback if a 21-digit station outputs centesimal 1e-4 Gon in 7-digit angle fields
            long vRaw;
            long hRaw;
            if (long.TryParse(digits.Substring(7, 7), NumberStyles.None, CultureInfo.InvariantCulture, out vRaw) &&
                long.TryParse(digits.Substring(14, 7), NumberStyles.None, CultureInfo.InvariantCulture, out hRaw))
            {
                return PolarReading.FromGon(
                    hRaw * 0.0001,
                    vRaw * 0.0001,
                    distMm * 0.001,
                    "Numeric-21 (Gon)",
                    digits.Substring(0, 21));
            }

            return null;
        }

        private static bool TryDecodePackedDms7(string sevenDigits, out double decimalDegrees)
        {
            decimalDegrees = 0.0;
            if (sevenDigits == null || sevenDigits.Length != 7) return false;

            int deg;
            int min;
            int sec;
            if (!int.TryParse(sevenDigits.Substring(0, 3), NumberStyles.None, CultureInfo.InvariantCulture, out deg) ||
                !int.TryParse(sevenDigits.Substring(3, 2), NumberStyles.None, CultureInfo.InvariantCulture, out min) ||
                !int.TryParse(sevenDigits.Substring(5, 2), NumberStyles.None, CultureInfo.InvariantCulture, out sec))
            {
                return false;
            }

            if (deg > 360 || min >= 60 || sec >= 60)
            {
                return false;
            }

            decimalDegrees = deg + (min / 60.0) + (sec / 3600.0);
            return true;
        }

        /// <summary>
        /// Decodes Sokkia / SET 23-digit continuous frame:
        /// [0..6] (7 digits) = Slope Distance in mm
        /// [7..14] (8 digits) = Vertical Angle in 10^-4 Gon
        /// [15..22] (8 digits) = Horizontal Angle in 10^-4 Gon
        /// </summary>
        private static PolarReading TryParseSet23Block(string input)
        {
            string digits = ExtractLeadingContiguousDigits(input);
            if (digits.Length < 23) return null;

            long distMm;
            long vertRaw;
            long horizRaw;

            if (!long.TryParse(digits.Substring(0, 7), NumberStyles.None, CultureInfo.InvariantCulture, out distMm) ||
                !long.TryParse(digits.Substring(7, 8), NumberStyles.None, CultureInfo.InvariantCulture, out vertRaw) ||
                !long.TryParse(digits.Substring(15, 8), NumberStyles.None, CultureInfo.InvariantCulture, out horizRaw))
            {
                return null;
            }

            return PolarReading.FromGon(
                horizRaw * 0.0001,
                vertRaw * 0.0001,
                distMm * 0.001,
                "Sokkia SET (23d Gon)",
                digits.Substring(0, 23));
        }

        /// <summary>
        /// Decodes Leica / Stonex / GeoMax GSI-8 and GSI-16 packets by word identifier
        /// (WI 21 = Horizontal Angle, WI 22 = Vertical Angle, WI 31 = Slope Distance).
        /// </summary>
        private static PolarReading TryParseGsiWordPacket(string input)
        {
            string trimmed = input.TrimStart('*').Trim();
            string[] tokens = trimmed.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 3) return null;

            bool hasHz = false;
            bool hasVert = false;
            bool hasDist = false;

            double hzGon = 0.0;
            double vertGon = 0.0;
            double slopeMeters = 0.0;

            for (int i = 0; i < tokens.Length; i++)
            {
                string word = tokens[i];
                if (word.Length < 7) continue;

                string wordId = word.Substring(0, 2);
                int signPos = word.IndexOfAny(new char[] { '+', '-' });
                if (signPos < 0 || signPos + 1 >= word.Length) continue;

                char unitCode = (signPos >= 1) ? word[signPos - 1] : '2';
                string payload = word.Substring(signPos + 1);

                long numericValue;
                if (!long.TryParse(payload, NumberStyles.Integer, CultureInfo.InvariantCulture, out numericValue))
                {
                    continue;
                }

                if (wordId == "21")
                {
                    hzGon = numericValue * 0.0001;
                    hasHz = true;
                }
                else if (wordId == "22")
                {
                    vertGon = numericValue * 0.0001;
                    hasVert = true;
                }
                else if (wordId == "31")
                {
                    double scale = 0.001;
                    if (unitCode == '6') scale = 0.0001;
                    else if (unitCode == '8') scale = 0.00001;
                    slopeMeters = numericValue * scale;
                    hasDist = true;
                }
            }

            if (!hasHz || !hasVert || !hasDist) return null;

            return PolarReading.FromGon(hzGon, vertGon, slopeMeters, "Leica/Stonex GSI (Gon)", input);
        }

        private static bool ContainsLabeledTokens(string text)
        {
            return text.IndexOf("SD", StringComparison.OrdinalIgnoreCase) >= 0 &&
                   text.IndexOf("HA", StringComparison.OrdinalIgnoreCase) >= 0 &&
                   text.IndexOf("VA", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Decodes Nikon / key-value ASCII telemetry packets containing SD, HA, and VA fields.
        /// </summary>
        private static PolarReading TryParseKeyValuePacket(string input)
        {
            double slopeMeters = -1.0;
            double hzGon = -1.0;
            double vertGon = -1.0;

            string[] tokens = input.Split(new char[] { ' ', ',', '\t', ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < tokens.Length; i++)
            {
                string tok = tokens[i];
                if (tok.Length < 4) continue;

                if (tok.StartsWith("SD", StringComparison.OrdinalIgnoreCase))
                {
                    slopeMeters = ParseTrailingDouble(tok.Substring(3)) * 0.001;
                }
                else if (tok.StartsWith("HA", StringComparison.OrdinalIgnoreCase))
                {
                    hzGon = ParseTrailingDouble(tok.Substring(3)) * 0.0001;
                }
                else if (tok.StartsWith("VA", StringComparison.OrdinalIgnoreCase))
                {
                    vertGon = ParseTrailingDouble(tok.Substring(3)) * 0.0001;
                }
            }

            if (slopeMeters >= 0.0 && hzGon >= 0.0 && vertGon >= 0.0)
            {
                return PolarReading.FromGon(hzGon, vertGon, slopeMeters, "Nikon Raw ASCII", input);
            }
            return null;
        }

        private static double ParseTrailingDouble(string rawSlice)
        {
            if (string.IsNullOrEmpty(rawSlice)) return -1.0;
            string trimmed = rawSlice.Trim();
            double value;
            if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return value;
            }
            return -1.0;
        }
    }
}
