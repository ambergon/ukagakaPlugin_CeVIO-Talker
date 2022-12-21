








using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

using System.Threading;
using CeVIO.Talk.RemoteService2;
//  csc /r:"C:\Program Files/\CeVIO\CeVIO AI\CeVIO.Talk.RemoteService2.dll"

//match
using System.Text.RegularExpressions;
namespace MyServer {
    class Program {
        private static int numThreads = 8;

        static void Main(string[] args) {
            // 【CeVIO AI】開始
            ServiceControl2.StartHost(false);
            CreatePipeServerTask("UkagakaPlugin/CeVIO_Talker").Wait();
            // 【CeVIO AI】終了
            ServiceControl2.CloseHost();
        }

        public static Task CreatePipeServerTask(string pipeName) {


            return Task.Run(() => {
                NamedPipeServerStream pipeServer = null;

                while ( true ) {
                    try {
                        pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, numThreads);

                        // クライアントの接続待ち
                        pipeServer.WaitForConnection();
                        StreamString ss = new StreamString(pipeServer);
                        CeVIO cevio = new CeVIO();


                        while ( true ) {


                            string writeData = "";
                            var readData  = ss.ReadString();

                            //終了項目
                            if ( readData == "end") {
                                ss.WriteString( "endClient" );
                                break;


                            //使用可能なキャラリストを取得する
                            } else if ( readData == "GetCharList") {
                                writeData = cevio.GetCharList();
                                ss.WriteString( writeData );


                            //指定したキャラの調声箇所一覧
                            } else if ( Regex.IsMatch( readData , "^GetCharOptions_") ) {
                                string CharName = readData.Replace( "GetCharOptions_" , "" );
                                writeData = cevio.GetCharOptions( CharName );
                                ss.WriteString( writeData );


                            //トーク
                            } else {
                                writeData = "Server read OK.";
                                ss.WriteString( writeData );
                                cevio.talk( readData );
                            }


                        }


                        break;
                    // クライアントが切断
                    } catch (OverflowException e) {
                        //Console.WriteLine( e.Message );
                    } finally {
                        pipeServer.Close();
                    }
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




    public class CeVIO {

        //使用可能なキャラリストを取得
        public string GetCharList() {
            string Char = "";
            string[] CharList = Talker2.AvailableCasts;
            foreach( string charctor in CharList ){
                Char = Char + "," + charctor;
            }
            return Char;
        }


        //キャラごとの設定項目を取得
        public string GetCharOptions( string charName ) {
            Talker2 talker = new Talker2();
            talker.Cast = charName;
            TalkerComponentCollection2 CharStatus = talker.Components;


            //項目数
            int Count = CharStatus.Count;
            string Options = "";
            for( int i = 0 ; i < Count ; i++ ){
                string Option = CharStatus[i].Name;
                Options = Options + "," + Option;
            }
            return Options;
        }


        public void talk(string arg) {
            ServiceControl2.StartHost(false);
            Talker2 talker = new Talker2();

            char[] sep = new char[] { ',' };
            //まずは頭を一つ
            string[] head = arg.Split( sep , 2);
            string CharName = head[0];
            talker.Cast = CharName;
            Console.WriteLine( "Char = " + CharName);

            TalkerComponentCollection2 CharStatus = talker.Components;
            int CharOptionNum = CharStatus.Count;
            int AllOptionNum = 1 + 5 + CharOptionNum + 1;

            string[] Options = arg.Split( sep , AllOptionNum );

            //共通項が5
            talker.Volume    = Convert.ToUInt32(Options[1]); 
            talker.Speed     = Convert.ToUInt32(Options[2]); 
            talker.Tone      = Convert.ToUInt32(Options[3]); 
            talker.Alpha     = Convert.ToUInt32(Options[4]); 
            talker.ToneScale = Convert.ToUInt32(Options[5]); 

            Console.WriteLine( "Volume    = " + Options[1]);
            Console.WriteLine( "Speed     = " + Options[2]);
            Console.WriteLine( "Tone      = " + Options[3]);
            Console.WriteLine( "Alpha     = " + Options[4]);
            Console.WriteLine( "ToneScale = " + Options[5]);
            
            
            string[] MySep = {"MySep"};
            for( int i = 0 ; i < CharOptionNum ; i++ ){
                int a = i + 6;
                if ( Options[a] != "" ) {
                    //string str = "哀しみMySep0";
                    string CharOption = Options[a];
                    string[] OptionMap = CharOption.Split( MySep , StringSplitOptions.None);
                    string optionName  = OptionMap[0] ;
                    uint   optionValue = (uint)Convert.ToUInt32(OptionMap[1]);
                    CharStatus[optionName].Value = optionValue;
                    Console.WriteLine( OptionMap[0] + " = " + OptionMap[1]);
                    
                } else {
                    Console.WriteLine( "Option無し" );
                }
            }
            //CharStatus[0].Value = 100;
            //CharStatus["Bright"].Value   = 100;
            

            //200文字問題
            string   VoiceText   = Options[AllOptionNum-1];
            string CheckText = VoiceText.Replace("。","");
            CheckText = CheckText.Replace("、","");
            CheckText = CheckText.Replace("　","");
            CheckText = CheckText.Replace(" ","");
            //バルーン空打ち対策
            if ( CheckText != "" ) {
                talker.Stop();
                SpeakingState2 state = talker.Speak( VoiceText );
                Console.WriteLine( VoiceText );
            }


            //string[] VoiceSep = {"、" , "。"};
            //string[] VoiceTexts = VoiceText.Split( VoiceSep , StringSplitOptions.None);
            //foreach( string Line in VoiceTexts ){
            //    if ( Line != "" ) {
            //        Console.WriteLine( Line );
            //        talker.Stop();
            //        SpeakingState2 state = talker.Speak( Line );
            //    }
            //}


            //SpeakingState2 state = talker.Speak( VoiceText );
            //state.Wait();
            //Thread.Sleep(2000);
            
        }
    }
}







