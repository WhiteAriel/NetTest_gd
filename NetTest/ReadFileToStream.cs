using System;
using System.IO;


namespace NetTest
{
    class ReadFileToStream
    {
        private string m_filename;
        private Stream m_stream;

        public ReadFileToStream(Stream stream, string filename)
        {
            m_filename = filename;
            m_stream = stream;
        } // ReadFileToStream(stream, filename)

        public void Start()
        {
            StreamWriter sw = new StreamWriter(m_stream);
            string contents = File.ReadAllText(m_filename);
            sw.WriteLine(contents);
            sw.Flush();
        } // Start()

    }
}
