using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Diva.Utils
{
    public class CSVData : IDisposable
    {
        private StreamReader m_sr;
        private string[] headers;

        public CSVData(string pathToFile)
        {
            m_sr = new StreamReader(pathToFile);
            // Read first line -- header
            string line = m_sr.ReadLine().ToLower();
            headers = line.Split(new char[] {',', ';', '\t'});
        }

        public string[] NextRow()
        {
            if (m_sr.Peek() >= 0)
                return m_sr.ReadLine().Split(new char[] { ',', ';', '\t' });

            return null;
        }

        public int IndexOf(string header)
        {
            int i = 0;
            foreach (string h in headers)
            {
                if (h.Contains(header.ToLower()))
                    return i;
                i++;
            }
            return -1;
        }

        public void Dispose()
        {
            if (m_sr != null)
                m_sr.Close();
        }
    }
}
