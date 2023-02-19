using System.IO;
using System.Text;

public static class TextReadOrWriter
{
    public static void CreateText(string path)
    {
        try
        {
            using (new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 2048, FileOptions.None)) { }
        }
        catch (System.Exception e)
        {

        }
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
        try
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 2048, FileOptions.None))
            {
                using (TextWriter sw = new StreamWriter(stream, Encoding.UTF8, 2048, false))
                {
                    sw.Write(content);
                }
            }
        }
        catch (System.Exception e)
        {

        }

    }

    public static string ReadText(string path)
    {
        //if (string.IsNullOrEmpty(path))
        //{
        //    return null;
        //}

        try
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var sr = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, 2048, true))
                {
                    return sr.ReadToEnd();
                }
            }
        }
        catch (System.Exception e)
        {
            return string.Empty;
        }
    }

    public static void WriteText(string path, string content)
    {
        //if (string.IsNullOrEmpty(path))
        //{
        //    return;
        //}
        try
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read))
            {
                if (content.Length == 0)
                {
                    stream.SetLength(0);
                }
                else
                {
                    using (TextWriter sw = new StreamWriter(stream, Encoding.UTF8, 2048, false))
                    {
                        sw.Write(content);
                    }
                }

            }
        }
        catch (System.Exception e)
        {

        }
    }
}
