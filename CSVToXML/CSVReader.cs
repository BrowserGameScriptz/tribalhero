#region

using System;
using System.IO;

#endregion

namespace CSVToXML {
    public class CSVReader : IDisposable {
        private string[] columns;
        private StreamReader sr = null;

        public string[] Columns {
            get { return columns; }
        }

        public CSVReader(StreamReader sr) {
            this.sr = sr;

            columns = TokenizeCSVLine(sr.ReadLine());
        }

        public string[] ReadRow() {
            string line = string.Empty;
            while (true) {
                line = sr.ReadLine();

                if (line == null)
                    return null;

                line = line.Trim();

                if (line != string.Empty && !line.StartsWith("//"))
                    break;
            }

            return TokenizeCSVLine(line);
        }

        private string[] TokenizeCSVLine(string line) {
            string[] cells = line.Split(',');

            string[] result = new string[cells.Length];
            for (int i = 0; i < cells.Length; i++) {
                string cell = cells[i].Trim();
                if (cell.StartsWith("\""))
                    result[i] = cell.Replace("\"\"", "\"").Trim('"');
                else
                    result[i] = cell;
            }

            return result;
        }

        #region IDisposable Members

        public void Dispose() {}

        #endregion
    }
}