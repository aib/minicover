﻿using System;
using System.Collections.Generic;
using MiniCover.Model;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MiniCover.Reports
{
    public class HtmlReport : BaseReport
    {
        private const string BgColorGreen = "background-color: #D2EACE;";
        private const string BgColorRed = "background-color: #EACECC;";
        private const string BgColorBlue = "background-color: #EEF4ED;";
        private readonly string _output;
        private readonly StringBuilder _htmlReport;

        public HtmlReport(string output)
        {
            _output = output;
            _htmlReport = new StringBuilder();
        }

        protected override void SetFileColumnLength(int fileColumnsLength)
        {
        }

        protected override void WriteHeader()
        {
        }

        protected override void WriteReport(KeyValuePair<string, SourceFile> kvFile, int lines, int coveredLines, float coveragePercentage, ConsoleColor color)
        {
            _htmlReport.AppendLine("<tr>");
            _htmlReport.AppendLine($"<td><a href=\"{GetHtmlFileName(kvFile.Key)}\">{kvFile.Key}</a></td>");
            _htmlReport.AppendLine($"<td>{lines}</td>");
            _htmlReport.AppendLine($"<td>{coveredLines}</td>");
            _htmlReport.AppendLine($"<td style=\"{GetBgColor(color)}\">{coveragePercentage:P}</td>");
            _htmlReport.AppendLine("</tr>");
        }

        protected override void WriteDetailedReport(InstrumentationResult result, IDictionary<string, SourceFile> files, HashSet<int> hits)
        {
            foreach (var kvFile in files)
            {
                var lines = File.ReadAllLines(Path.Combine(result.SourcePath, kvFile.Key));

                var fileName = GetHtmlFileName(kvFile.Key);

                Directory.CreateDirectory(Path.GetDirectoryName(fileName));

                using (var htmlWriter = (TextWriter)File.CreateText(fileName))
                {
                    htmlWriter.WriteLine("<html>");
                    htmlWriter.WriteLine("<body style=\"font-family: monospace;\">");

                    var uncoveredLineNumbers = new HashSet<int>();
                    var coveredLineNumbers = new HashSet<int>();
                    foreach (var i in kvFile.Value.Instructions)
                    {
                        if (hits.Contains(i.Id))
                        {
                            coveredLineNumbers.UnionWith(i.GetLines());
                        }
                        else
                        {
                            uncoveredLineNumbers.UnionWith(i.GetLines());
                        }
                    }

                    var l = 0;
                    foreach (var line in lines)
                    {
                        l++;
                        var style = "white-space: pre;";
                        if (coveredLineNumbers.Contains(l))
                        {
                            style += BgColorGreen;
                        }
                        else if (uncoveredLineNumbers.Contains(l))
                        {
                            style += BgColorRed;
                        }
                        else
                        {
                            style += BgColorBlue;
                        }

                        if (!string.IsNullOrEmpty(line))
                        {
                            htmlWriter.WriteLine($"<div style=\"{style}\">{WebUtility.HtmlEncode(line)}</div>");
                        }
                        else
                        {
                            htmlWriter.WriteLine($"<div style=\"{style}\">&nbsp;</div>");
                        }
                    }

                    htmlWriter.WriteLine("</body>");
                    htmlWriter.WriteLine("</html>");
                }
            }
        }

        protected override void WriteFooter(int lines, int coveredLines, float coveragePercentage, float threshold, ConsoleColor color)
        {
            var result = new StringBuilder();

            result.AppendLine("<html>");
            result.AppendLine("<body style=\"font-family: sans-serif;\">");

            // Write summary
            result.AppendLine("<h1>Summary</h1>");
            result.AppendLine("<table border=\"1\" cellpadding=\"5\">");
            result.AppendLine($"<tr><th>Generated on</th><td>{DateTime.Now}</td></tr>");
            result.AppendLine($"<tr><th>Lines</th><td>{lines}</td></tr>");
            result.AppendLine($"<tr><th>Covered Lines</th><td>{coveredLines}</td></tr>");
            result.AppendLine($"<tr><th>Threshold</th><td>{threshold:P}</td></tr>");
            result.AppendLine($"<tr><th>Percentage</th><td style=\"{GetBgColor(color)}\">{coveragePercentage:P}</td></tr>");
            result.AppendLine("</table>");

            // Write detailed report
            result.AppendLine("<h1>Coverage</h1>");
            result.AppendLine("<table border=\"1\" cellpadding=\"5\">");
            result.AppendLine("<tr>");
            result.AppendLine("<th>File</th>");
            result.AppendLine("<th>Lines</th>");
            result.AppendLine("<th>Covered Lines</th>");
            result.AppendLine("<th>Percentage</th>");
            result.AppendLine("</tr>");
            result.Append(_htmlReport);
            result.AppendLine("</table>");
            result.AppendLine("</body>");
            result.AppendLine("</html>");

            Directory.CreateDirectory(_output);
            var fileName = Path.Combine(_output, "index.html");
            using (var htmlWriter = (TextWriter)File.CreateText(fileName))
            {
                htmlWriter.WriteLine(result.ToString());
            }
        }

        private string GetBgColor(ConsoleColor color)
        {
            switch (color)
            {
                case ConsoleColor.Green:
                    return BgColorGreen;
                case ConsoleColor.Red:
                    return BgColorRed;
                default:
                    throw new ArgumentException($"Invalid color: {color}");
            }
        }

        private string GetHtmlFileName(string fileName)
        {
            string safeName = Regex.Replace(fileName, @"^[./\\]+", "");
            return Path.Combine(_output, safeName + ".html");
        }
    }
}
