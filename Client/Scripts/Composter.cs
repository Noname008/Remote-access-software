using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Text;

namespace Client.Scripts
{
    public class Composter
    {
        private const byte type = 0xF0;
        readonly private int bufferPixels;
        readonly private int flowQuantity;
        readonly private byte[] bufferOut, bufferIn, change;

        public Composter(int Width, int Height)
        {
            this.flowQuantity = Environment.ProcessorCount;
            this.bufferPixels = Width * Height * 4 / flowQuantity + (Width * Height * 4 % flowQuantity == 0 ? 0 : 1);
            this.bufferOut = new byte[Width * Height * 4];
            this.bufferIn = new byte[Width * Height * 4];
            this.change = new byte[Width * Height * 4];
            Parallel.For(0, Width * Height * 4, i =>
            {
                this.bufferOut[i] = 0x00;
                this.bufferIn[i] = 0x00;
                this.change[i] = 0x00;
            });
        }

        public void DistributedCompression(byte[] data, Action<string, Commands> delegat)
        {
            Parallel.For(0, flowQuantity, i =>
            {
                for (int j = i * bufferPixels; j < (i + 1) * bufferPixels; j++)
                {
                    change[j] = (byte)((data[j] & type) ^ (bufferOut[j] & type));
                    bufferOut[j] = (byte)(data[j] & type);
                }
            });

            Compress(change, out string output);
            if(output.Length > 0)
            {
                delegat.Invoke(output, Commands.VideoData);
            }
        }

        public void Compress(byte[] data, out string output_s)
        {
            MemoryStream output = new MemoryStream();
            using (var dstream = new DeflateStream(output, CompressionLevel.Optimal))
            {
                dstream.Write(data, 0, data.Length);
            }
            output_s = Convert.ToBase64String(output.ToArray());
        }

        public byte[] Decompress(Stream data)
        {
            MemoryStream output = new MemoryStream();

            using (var dstream = new DeflateStream(data, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
            }

            byte[] result = output.ToArray();

            Parallel.For(0, flowQuantity, i =>
            {
                for (int j = i * bufferPixels; j < (i + 1) * bufferPixels && j < bufferIn.Length; j++)
                {
                    bufferIn[j] = (byte)((result[j] & type) ^ (bufferIn[j] & type));
                }
            });
            return bufferIn;
        }
    }
}