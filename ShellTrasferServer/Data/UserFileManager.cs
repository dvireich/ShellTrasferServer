using PostSharp.Patterns.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
    [Log(AttributeExclude = true)]
    public class UserFileManager : IUserFileManager
    {
        public FileStream FileStream { get; set; }
        public bool IsDownloding { get; set; }
        public bool Buffering { get; set; }
        public bool Error { get; set; }
        public string ErrorMessage { get; set; }
        public string FileSize { get; set; }
        private IEnumerator<Tuple<byte[], int>> _enumerableChunk = null;
        public IEnumerator<Tuple<byte[], int>> EnumerableChunk
        {
            get
            {
                if (_enumerableChunk == null)
                {
                    _enumerableChunk = GetNextChunk(Path).GetEnumerator();
                }
                return _enumerableChunk;
            }
        }
        public int Chunk { get { return 9999999; } }
        public string Path { set; get; }
        public long ReadSoFar { get; set; }
        public bool IsUploading { get; set; }
        public bool UploadingEnded { get; set; }

        private IEnumerable<Tuple<byte[], int>> GetNextChunk(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                ReadSoFar = 0;
                while (true)
                {
                    var buffer = new byte[Chunk];
                    var n = fs.Read(buffer, 0, buffer.Length);
                    if (n == 0)
                    {
                        if (_enumerableChunk != null)
                        {
                            _enumerableChunk.Dispose();
                            _enumerableChunk = null;
                        }

                        IsDownloding = false;
                        IsUploading = false;
                        fs.Close();
                        yield break;
                    }
                    ReadSoFar += n;
                    yield return new Tuple<byte[], int>(buffer, n);
                }
            }
        }
    }
}
