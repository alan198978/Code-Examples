using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Server
{
    static void Main(string[] args)
    {
        // 設定伺服器端 IP 和埠號
        int port = 5000;
        TcpListener server = new TcpListener(IPAddress.Any, port);

        server.Start();
        Console.WriteLine("伺服器已啟動，請輸入要傳輸的檔案路徑：");

        // 讓管理員輸入檔案路徑
        string 檔案路徑 = Console.ReadLine();
        string 檔案名稱 = Path.GetFileName(檔案路徑); // 提取檔案名稱

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine("客戶端已連接！");

            NetworkStream stream = client.GetStream();

            // 讀取客戶端的請求
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string 請求字 = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            Console.WriteLine("收到請求：" + 請求字);

            if (請求字 == "1")
            {
                // 客戶端要求下載檔案
                if (File.Exists(檔案路徑))
                {
                    byte[] 檔案資料內容 = File.ReadAllBytes(檔案路徑);
                    byte[] fileNameBytes = Encoding.ASCII.GetBytes(檔案名稱); // 檔案名稱的 byte 格式

                    // 傳送檔案名稱長度及檔案名稱給客戶端
                    byte[] fileNameLength = BitConverter.GetBytes(fileNameBytes.Length);
                    stream.Write(fileNameLength, 0, fileNameLength.Length); // 傳送檔名長度
                    stream.Write(fileNameBytes, 0, fileNameBytes.Length); // 傳送檔案名稱

                    // 傳送檔案大小給客戶端
                    byte[] 檔案大小 = BitConverter.GetBytes(檔案資料內容.Length);
                    stream.Write(檔案大小, 0, 檔案大小.Length);

                    // 傳送檔案內容
                    stream.Write(檔案資料內容, 0, 檔案資料內容.Length);
                    Console.WriteLine("檔案已傳送給客戶端。");
                }
                else
                {
                    Console.WriteLine("檔案不存在，無法傳送。");
                }
            }

            client.Close();
        }
    }
}
