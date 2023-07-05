using System.IO;
using UnityEngine;

namespace Recycle_N_Reclaim.GamePatches;

public static class Utils
{
    public static Texture2D LoadTextureFromResources(string pathName, string folderName = "Assets")
    {
        var myAssembly = typeof(Recycle_N_ReclaimPlugin).Assembly;
        var myStream = myAssembly.GetManifestResourceStream($"{typeof(Recycle_N_ReclaimPlugin).Namespace}.{folderName}.{pathName}");
        byte[] bytes;
        var tex2D = new Texture2D(2, 2);
        var emptyTex2D = new Texture2D(2, 2);

        if (myStream == null)
        {
            return emptyTex2D;
        }

        using (var binaryReader = new BinaryReader(myStream))
        {
            bytes = binaryReader.ReadBytes((int)myStream.Length);
        }

        return tex2D.LoadImage(bytes) ? tex2D : emptyTex2D;
    }
}