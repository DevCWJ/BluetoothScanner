

using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

public static class TextReadOrWriter
{
    public static void CreateText(string path)
    {
        using (new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024, FileOptions.None)) { }
    }

    public static void CreateOrWriteText(string path, string content)
    {
        if (!File.Exists(path))
        {
            if (content.Length == 0)
                CreateText(path);
            else
                CreateAndWriteText(path, content);
        }
        else
            WriteText(path, content);
    }

    public static void CreateAndWriteText(string path, string content)
    {
        using (var stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024, FileOptions.None))
        {
            using (TextWriter sw = new StreamWriter(stream, Encoding.UTF8, 1024, false))
            {
                sw.Write(content);
            }
        }
    }

    public static string ReadText(string path)
    {
        //if (string.IsNullOrEmpty(path))
        //{
        //    return null;
        //}
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
        {
            using (var sr = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, 1024, true))
            {
                return sr.ReadToEnd();
            }
        }
    }

    public static void WriteText(string path , string content)
    {
        //if (string.IsNullOrEmpty(path))
        //{
        //    return;
        //}
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write))
        {
            if (content.Length == 0)
            {
                stream.SetLength(0);
            }
            else
            {
                using (TextWriter sw = new StreamWriter(stream, Encoding.UTF8, 1024, false))
                {
                    sw.Write(content);
                }
            }

        }
    }
}
