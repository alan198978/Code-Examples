//本範例使用ubuntu系統來設置OpenSSH Server，C#客戶端在Windows上執行。
using Renci.SshNet;
using System.Net.Sockets;
using Renci.SshNet.Common;
using System.Threading;

namespace SFTPFileTransferExample
{
    class Program
    {
        static void Main(string[] args)
        {
            string host = "124.218.224.31"; //SSH Server IP地址。
            int port = 22; //SSH Server預設端口為22。
            string username = "alanlin"; //遠端主機的用戶名稱

            Console.Write("輸入伺服器端用戶密碼： ");
            string password = 讀取密碼函數();

            using (var 客戶端 = new SftpClient(host, port, username, password))
            {
                try
                {
                    Console.WriteLine("Connecting to SFTP server...");
                    客戶端.ConnectionInfo.Timeout = TimeSpan.FromSeconds(10);
                    客戶端.Connect();
                    Console.WriteLine("Connected!");

                    while (true)
                    {
                        Console.WriteLine("\n選擇操作：");
                        Console.WriteLine("1. 列出文件");
                        Console.WriteLine("2. 上傳文件");
                        Console.WriteLine("3. 下載文件");
                        Console.WriteLine("4. 退出");
                        Console.Write("輸入您的選擇 (1-4): ");

                        string choice = Console.ReadLine();

                        switch (choice)
                        {
                            case "1":
                                ListFiles(客戶端);
                                break;
                            case "2":
                                UploadFile(客戶端);
                                break;
                            case "3":
                                DownloadFile(客戶端);
                                break;
                            case "4":
                                客戶端.Dispose();
                                return;
                            default:
                                Console.WriteLine("無效選擇。請再試一次。");
                                break;
                        }
                    }
                }
                catch (SocketException ex)
                {
                    客戶端.Dispose();
                    Console.WriteLine($"發生套接字錯誤： {ex.Message}");
                    Console.WriteLine($"錯誤代碼： {ex.SocketErrorCode}");
                }
                catch (SshException ex)
                {
                    客戶端.Dispose();
                    Console.WriteLine($"發生 SSH 錯誤： {ex.Message}");
                }
                catch (Exception ex)
                {
                    客戶端.Dispose();
                    Console.WriteLine($"發生錯誤： {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"內部異常： {ex.InnerException.Message}");
                    }
                }
                finally
                {
                    //客戶端.Dispose();
                }
            }
            Console.WriteLine("按任意鍵退出...");
            Console.ReadKey();
        }

        static void ListFiles(SftpClient client)
        {
            Console.Write("輸入遠端目錄路徑（或按 Enter 鍵選擇目前目錄）： ");
            string path = Console.ReadLine() ?? string.Empty;
            var files = client.ListDirectory(path);
            foreach (var file in files)
            {
                Console.WriteLine($"{file.Name} - {file.Length} bytes");
            }
        }

        static void UploadFile(SftpClient client)
        {
            Console.Write("輸入要上傳的本機檔案路徑： ");
            string 本地路徑 = Console.ReadLine();
            Console.Write("輸入遠端檔案儲存路徑： ");
            string 遠端路徑 = Console.ReadLine();

            const int 分段大小 = 32 * 1024; // 32KB 分段傳輸大小
            const int 分段傳輸間隔時間 = 1000; // 每次分段傳輸間隔1000毫秒
            //上述來說，傳輸速度相當於每秒32KB....，非常慢，
            //我目前還不知道如何繞開Windows防火牆流量限制。
            try
            {
                using (var 檔案流 = new FileStream(本地路徑, FileMode.Open, FileAccess.Read))
                {
                    long 檔案大小 = 檔案流.Length;
                    byte[] 記憶體緩存大小 = new byte[分段大小];
                    long 讀取總位元組數 = 0;
                    int 當前位元組讀取數;

                    Console.WriteLine($"準備將檔案分塊上傳到 {遠端路徑}...");

                    using (var 遠端檔案 = client.OpenWrite(遠端路徑))
                    {
                        while ((當前位元組讀取數 = 檔案流.Read(記憶體緩存大小, 0, 分段大小)) > 0)
                        {
                            遠端檔案.Write(記憶體緩存大小, 0, 當前位元組讀取數);
                            讀取總位元組數 += 當前位元組讀取數;
                            double 進度 = (double)讀取總位元組數 / 檔案大小 * 100;
                            Console.WriteLine($"進度: {進度:F2}%");

                            // 區塊之間暫停
                            Thread.Sleep(分段傳輸間隔時間);

                            // 如果會話逾時則重新連接
                            if (!client.IsConnected)
                            {
                                Console.WriteLine("正在重新連接到 SFTP 伺服器...");
                                client.Connect();
                            }
                        }
                        遠端檔案.Flush(); //.Flush()用於強制將緩存區中的數據寫入到基礎存儲（如硬盤或遠端伺服器）中。
                        //如果沒有調用 Flush()，有可能部分數據仍留在緩存中，導致文件內容不完整或延遲寫入。
                    }
                    Console.WriteLine("文件已成功分塊上傳。");
                    client.Disconnect();
                }
            }
            catch (Exception ex)
            {
                client.Disconnect();
                client.Dispose();
                Console.WriteLine($"Error during file upload: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }

        static void DownloadFile(SftpClient client)
        {
            //Ubuntu的防火牆沒有流量限制，所以從服務器端下載到客戶端速度會比較快。
            Console.Write("輸入要下載的遠端檔案路徑： ");
            string 遠端路徑 = Console.ReadLine();
            Console.Write("輸入本機檔案儲存路徑： ");
            string 本地路徑 = Console.ReadLine();

            try
            {
                using (var 檔案流 = new FileStream(本地路徑, FileMode.Create))
                {
                    Console.WriteLine($"準備從 {遠端路徑} 下載檔案...");

                    var 檔案大小 = client.GetAttributes(遠端路徑).Size;
                    var 非同步結果 = client.BeginDownloadFile(遠端路徑, 檔案流);
                    //BeginDownloadFile() 是一個用於啟動異步下載文件的非阻塞方法。

                    while (!非同步結果.IsCompleted) //IAsyncResult接口可以調用IsCompleted屬性，用來檢查異步操作是否完成。
                    {
                        long 下載位元組數 = 檔案流.Length;
                        double 進度 = (double)下載位元組數 / 檔案大小 * 100;
                        Console.Write($"\r進度: {進度:F2}%");
                        System.Threading.Thread.Sleep(100); // 每100ms更新一次進度
                    }

                    client.EndDownloadFile(非同步結果);
                    //EndDownloadFile() 用於結束通過 BeginDownloadFile() 發起的異步下載操作，並確保文件下載完成。
                    Console.WriteLine("\n文件下載成功。");
                }
            }
            catch (Exception ex)
            {
                client.Disconnect();
                client.Dispose();
                Console.WriteLine($"\n文件下載時發生錯誤： {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"內部異常： {ex.InnerException.Message}");
                }
            }
        }

        static string 讀取密碼函數()
        {
            string password = "";
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                    break;
                if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password = password.Substring(0, password.Length - 1);
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
            }
            Console.WriteLine();
            return password;
        }
    }
}
