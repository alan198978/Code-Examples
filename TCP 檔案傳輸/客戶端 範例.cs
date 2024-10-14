using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

class Client
{
    static void Main(string[] args)
    {
        // 連接伺服器端 IP 和埠號
        string serverIp = "192.168.18.4";
        int port = 5000;

        Console.WriteLine("請輸入 'y' 來建立伺服器的連接");
        string userInput1 = Console.ReadLine();

        if (userInput1 == "y")
        {
            try
            {
                // 建立與伺服器的連接
                TcpClient client = new TcpClient(serverIp, port);
                NetworkStream stream = client.GetStream();

                // 提示用戶是否要下載檔案
                Console.WriteLine("輸入 '1' 來下載伺服器上的檔案，或按其他鍵退出：");
                string userInput2 = Console.ReadLine();

                if (userInput2 == "1")
                {
                    // 傳送請求下載檔案
                    byte[] 請求數據 = Encoding.ASCII.GetBytes(userInput2);
                    stream.Write(請求數據, 0, 請求數據.Length); //.Write 向伺服器發送轉為byte[]的 "1" 訊息。

                    // 接收檔案名稱長度
                    byte[] 檔案名稱長度緩衝 = new byte[4];
                    stream.Read(檔案名稱長度緩衝, 0, 檔案名稱長度緩衝.Length);
                    int 檔案名稱長度整數值 = BitConverter.ToInt32(檔案名稱長度緩衝, 0); //使用 BitConverter.ToInt32 把 byte[] 長度值轉為 int。

                    // 接收檔案名稱
                    byte[] 檔案名稱緩衝 = new byte[檔案名稱長度整數值];
                    stream.Read(檔案名稱緩衝, 0, 檔案名稱緩衝.Length);
                    string 檔案名稱 = Encoding.ASCII.GetString(檔案名稱緩衝);
                    Console.WriteLine("下載的檔案名稱：" + 檔案名稱);

                    // 接收檔案大小
                    byte[] fileSizeBuffer = new byte[4];
                    stream.Read(fileSizeBuffer, 0, fileSizeBuffer.Length);
                    int fileSize = BitConverter.ToInt32(fileSizeBuffer, 0);

                    // 接收檔案內容
                    byte[] 檔案資料內容 = new byte[fileSize];
                    int bytesReceived = 0; //每次最初設定為 0，因為在開始接收之前，我們還沒有接收到任何資料。

                    while (bytesReceived < fileSize)
                    {
                        bytesReceived += stream.Read(檔案資料內容, bytesReceived, fileSize - bytesReceived);
                    }

                    // 動態取得使用者的 "Downloads" 資料夾路徑
                    string 動態Downloads路徑 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

                    // 儲存檔案到客戶端，使用從伺服器接收到的檔案名稱
                    string 儲存路徑 = Path.Combine(動態Downloads路徑, 檔案名稱);
                    File.WriteAllBytes(儲存路徑, 檔案資料內容);
                    Console.WriteLine("檔案已成功下載到：" + 儲存路徑);

                    client.Close();
                }
                else if(userInput2 == "1")
                {

                }
                else
                {
                    Console.WriteLine("未下載任何檔案。");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("發生錯誤：" + e.Message);
            }
        }

        Console.ReadLine(); // 留一個空白輸入，避免測試完畢時直接被關閉視窗。
    }
}
