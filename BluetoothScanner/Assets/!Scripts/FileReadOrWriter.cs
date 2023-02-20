using System.IO;
using System.Text;

public static class TextReadOrWriter
{
    public static void CreateText(string path, FileShare fileShare = FileShare.Read)
    {
        try
        {
            using (new FileStream(path, FileMode.Create, FileAccess.ReadWrite, fileShare, 2048, FileOptions.WriteThrough)) { }
        }
        catch (System.Exception e)
        {

        }
    }

    public static void CreateOrWriteText(string path, string content, FileShare fileShare = FileShare.Read)
    {
        if (!File.Exists(path))
        {
            if (content.Length == 0)
                CreateText(path, fileShare);
            else
                CreateAndWriteText(path, content, fileShare);
        }
        else
            WriteText(path, content, fileShare);
    }

    public static void CreateAndWriteText(string path, string content, FileShare fileShare = FileShare.Read)
    {
        try
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, fileShare, 2048, FileOptions.WriteThrough))
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

    public static string ReadText(string path, FileShare fileShare = FileShare.Read)
    {
        //if (string.IsNullOrEmpty(path))
        //{
        //    return null;
        //}

        try
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, fileShare))
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

    public static void WriteAppendText(string path, string content, FileShare fileShare = FileShare.Read)
    {
        try
        {
            using (var stream = new FileStream(path, FileMode.Append, FileAccess.Write, fileShare, 2048, FileOptions.WriteThrough))
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

    public static void WriteText(string path, string content, FileShare fileShare = FileShare.Read)
    {
        //if (string.IsNullOrEmpty(path))
        //{
        //    return;
        //}
        try
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write, fileShare, 2048, FileOptions.WriteThrough))
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
