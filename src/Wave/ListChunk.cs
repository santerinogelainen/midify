
using Midify.Helpers;
using System.Linq;

namespace Midify.WaveFile.Chunks {
    public class ListChunk : LittleEndianObjectStruct {

        public byte[] Prefix = new byte[4];
        public int Size = 0;

        public bool Read(AudioStream from) {

            from.Read(this);

#if DEBUG
            this.Debug();
#endif
            return true;
        }

        public static bool SkipAll(AudioStream from) {
            while (true) {
                ListChunk list = new ListChunk();
                list.Read(from);
                if (list.Prefix.SequenceEqual(new byte[4] { (byte)'L', (byte)'I', (byte)'S', (byte)'T' })) {
                    from.Skip(list.Size);
                } else {
                    from.Stream.Seek(-8, System.IO.SeekOrigin.Current);
                    break;
                }
            }
            return true;
        }

    }
}
