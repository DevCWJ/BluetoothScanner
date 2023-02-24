using System;
using System.IO;
using UnityEngine;

namespace CWJ.IoT
{
    [CreateAssetMenu(fileName = "my_certificate", menuName = "CWJ.IoT/Certificate", order = 0)]
    public class ScriptableCertificate : ScriptableObject
    {
        public string absPath;
        public string password;
        public string GetRelativePath()
        {
            if (absPath == null)
            {
                return null;
            }

            var relativePath = absPath.Equals(String.Empty)
                ? ""
                : Path.GetFullPath(absPath).Replace(Path.GetFullPath(Application.streamingAssetsPath), "");
            relativePath = relativePath.TrimStart('\\');
            if (Path.IsPathRooted(relativePath))
            {
                relativePath = Path.GetFullPath(absPath).Replace(Path.GetFullPath(Application.dataPath), "").TrimStart('\\');
            }
            relativePath = relativePath.TrimStart('/');
            if (relativePath.Equals(String.Empty))
            {
                return null;
            }

            return relativePath;
        }
    }
}