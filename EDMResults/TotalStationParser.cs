using System;
using System.Globalization;

namespace EDMResults.Core
{
    public static class TotalStationDecoder
    {
        public static SurveyPoint DecodeFrame(string rawLine)
        {
            if (string.IsNullOrEmpty(rawLine))
            {
                return new SurveyPoint(0, 0, -1);
            }

            char lead = rawLine[0];
            if (lead == '\x01')
            {
                return ParseNikonFrame(rawLine);
            }
            if (lead == '*')
            {
                return ParseStonexFrame(rawLine);
            }
            if (lead >= '0' && lead <= '9')
            {
                return ParseSokkiaSetFrame(rawLine);
            }

            return new SurveyPoint(0, 0, -1);
        }

        private static SurveyPoint ParseNikonFrame(string line)
        {
            double hzGon = 0.0;
            double vGon = 0.0;
            double distMm = -1.0;

            try
            {
                string[] tokens = line.Split(' ');
                for (int i = 0; i < tokens.Length; i++)
                {
                    string token = tokens[i];
                    if (token.Length < 12) continue;

                    string tag = token.Substring(0, 3);
                    int rawVal;
                    if (!int.TryParse(token.Substring(3, 9), out rawVal)) continue;

                    if (tag == "HA:")
                    {
                        hzGon = rawVal / 100000.0;
                    }
                    else if (tag == "VA#")
                    {
                        vGon = rawVal / 100000.0;
                    }
                    else if (tag == "SD:")
                    {
                        distMm = rawVal / 10.0;
                    }
                }
            }
            catch
            {
            }

            return new SurveyPoint(hzGon, vGon, distMm);
        }

        private static SurveyPoint ParseStonexFrame(string line)
        {
            double hzGon = 0.0;
            double vGon = 0.0;
            double distMm = -1.0;

            try
            {
                string payload = line.Length > 3 ? line.Substring(1, line.Length - 3) : line;
                string[] blocks = payload.Split(' ');

                for (int i = 0; i < blocks.Length; i++)
                {
                    string block = blocks[i];
                    if (block.Length < 23) continue;

                    int code;
                    int rawVal;
                    if (!int.TryParse(block.Substring(0, 2), out code)) continue;
                    if (!int.TryParse(block.Substring(6, 17), out rawVal)) continue;

                    if (code == 21)
                    {
                        hzGon = rawVal / 100000.0;
                    }
                    else if (code == 22)
                    {
                        vGon = rawVal / 100000.0;
                    }
                    else if (code == 31)
                    {
                        distMm = rawVal;
                    }
                }
            }
            catch
            {
            }

            return new SurveyPoint(hzGon, vGon, distMm);
        }

        private static SurveyPoint ParseSokkiaSetFrame(string line)
        {
            double hzGon = 0.0;
            double vGon = 0.0;
            double distMm = -1.0;

            try
            {
                string distSlice = ExtractNumericSlice(line, 1, 7);
                distMm = Convert.ToDouble(distSlice, CultureInfo.InvariantCulture);

                string vSlice = ExtractNumericSlice(line, 8, 15);
                vGon = Convert.ToDouble(vSlice, CultureInfo.InvariantCulture) / 10000.0;

                string hzSlice = ExtractNumericSlice(line, 16, 23);
                hzGon = Convert.ToDouble(hzSlice, CultureInfo.InvariantCulture) / 10000.0;
            }
            catch
            {
            }

            return new SurveyPoint(hzGon, vGon, distMm);
        }

        private static string ExtractNumericSlice(string source, int startIdx, int endIdx)
        {
            int cursor = startIdx;
            while (cursor < endIdx && cursor < source.Length && source[cursor] == '0')
            {
                cursor++;
            }
            if (cursor >= endIdx)
            {
                return "0";
            }
            return source.Substring(cursor, endIdx - cursor);
        }
    }
}

