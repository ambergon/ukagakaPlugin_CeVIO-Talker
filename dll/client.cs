







using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace MyClient {
    class Program {

        static void Main(string[] args) {
            //引数は一塊で受け取る。
            CreateClientTask("UkagakaPlugin/CeVIO_Talker" , args[0]).Wait();
        }

        public static Task CreateClientTask(string pipeName , string writeData ) {
            return Task.Run(() => {

                NamedPipeClientStream pipeClient = null;
                try {
                    pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation);
                    pipeClient.Connect();

                    var ss = new StreamString(pipeClient);
                    //var writeData = "ONE,50,50,50,50,50,100,0,0,0,すき";
                    //var writeData = Console.ReadLine();
                    ss.WriteString(writeData);
                    
                    // 応答待ち
                    var read = ss.ReadString();
                    //yayaに送り返すテキスト
                    Console.Write( read );
                    //WriteLineだと文末に空白が出来てしまう。
                    //Console.WriteLine( read );


                } catch (OverflowException ofex) {
                    Console.WriteLine(ofex.Message);
                } catch (IOException ioe) {
                    // 送信失敗
                    Console.WriteLine(ioe.Message);
                } finally {
                    pipeClient.Close();
                }


            });
        }
    }


    public class StreamString {
        private Stream ioStream;
        private UnicodeEncoding streamEncoding;

        public StreamString(Stream ioStream) {
            this.ioStream = ioStream;
            streamEncoding = new UnicodeEncoding();
        }

        public string ReadString() {
            int len = 0;

            len = ioStream.ReadByte() * 256;
            len += ioStream.ReadByte();
            byte[] inBuffer = new byte[len];
            ioStream.Read(inBuffer, 0, len);

            return streamEncoding.GetString(inBuffer);
        }

        public int WriteString(string outString) {
            byte[] outBuffer = streamEncoding.GetBytes(outString);
            int len = outBuffer.Length;
            if (len > UInt16.MaxValue)
            {
                len = (int)UInt16.MaxValue;
            }
            ioStream.WriteByte((byte)(len / 256));
            ioStream.WriteByte((byte)(len & 255));
            ioStream.Write(outBuffer, 0, len);
            ioStream.Flush();

            return outBuffer.Length + 2;
        }
    }
}
