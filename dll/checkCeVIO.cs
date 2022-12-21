




//cevioがインストールされているかどうかをこれでチェックする。
//True or False
using CeVIO.Talk.RemoteService2;
//  csc /r:"C:\Program Files/\CeVIO\CeVIO AI\CeVIO.Talk.RemoteService2.dll"


using System;
 
class Program {
    static void Main(string[] args) {

        //起動されているかどうか
        //bool Check = ServiceControl2.IsHostStarted;
        //Console.Write( Check );
        int Check = (int)ServiceControl2.StartHost(true);
        if ( Check == 0) {
            Console.Write("True");
        } else {
            Console.Write("False");
        }
    }
}
