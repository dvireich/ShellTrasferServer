using System;
using System.Collections.Generic;
using System.IO;

namespace Data
{
    public interface IUserFileManager
    {
        FileStream FileStream { get; set;}
        bool IsDownloding { get; set; }
        bool Buffering { get; set;}
        bool Error { get; set;}
        string ErrorMessage { get; set;}
        string FileSize { get; set;}
        int Chunk { get;}
        string Path { set; get;}
        long ReadSoFar { get; set;}
        bool IsUploading { get; set;}
        bool UploadingEnded { get; set;}

        IEnumerator<Tuple<byte[], int>> EnumerableChunk { get; }
    }
}
