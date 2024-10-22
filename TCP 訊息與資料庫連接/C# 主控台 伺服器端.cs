using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite; //導入必要命名空間
using System.Security.Cryptography; // 加入此命名空間用於雜湊加密

class Program
{
    // 保存所有連接的客戶端, 用來將每個連接的客戶端的暱稱（string）與其對應的 TcpClient 進行配對
    private static Dictionary<string, TcpClient> 客戶端列表 = new Dictionary<string, TcpClient>();

    static string 資料庫位址字串 = @"Data Source=C:\Users\使用者名稱\Desktop\資料庫\測試資料庫.db;";

    // 密碼雜湊方法
    private static string 加密密碼(string 原始密碼)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            // 將密碼轉換成 byte 陣列
            byte[] bytes = Encoding.UTF8.GetBytes(原始密碼);

            // 進行雜湊運算
            byte[] hash = sha256.ComputeHash(bytes);

            // 將雜湊結果轉換成十六進制字串
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                result.Append(hash[i].ToString("x2")); // x2 表示轉換為兩位數的十六進制
            }

            return result.ToString();
        }
    }

    static async Task Main(string[] args)
    {
        const int port = 5000;
        //建立一個 TCP 監聽器，並設定伺服器的監聽埠為 5000 (port)。IPAddress.Any表示伺服器可以接受來自任何 IP 地址的連接。
        var listener = new TcpListener(IPAddress.Any, port);

        try
        {
            listener.Start(); //啟動監聽器，開始等待客戶端連接。
            Console.WriteLine($"TCP Server is running on port {port}...");

            // 啟動一個獨立任務來處理伺服器輸入，允許伺服器管理員（您）手動輸入來向特定客戶端發送訊息。
            _ = Task.Run(() => HandleServerInput());

            while (true)
            {
                // AcceptTcpClientAsync()：伺服器會異步等待新的客戶端連線。一旦有新的客戶端連接，伺服器就會打印 "Client connected."。
                var client = await listener.AcceptTcpClientAsync();
                Console.WriteLine("Client connected.");

                _ = Task.Run(() => 異步客戶端訊息處理(client));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
        }
        finally
        {
            listener.Stop();
        }
    }

    private static async Task 異步客戶端訊息處理(TcpClient client)
    {
        using (var networkStream = client.GetStream()) //.GetStream()將指定某一客戶端來收發網路流。
        {
            var buffer = new byte[1024];
            int bytesRead; //當接收到資料時，會同時返回取得的位元組數量，將此數量存入此變數。

            try
            {
                // while迴圈來持續接收來自客戶端的訊息
                while ((bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    var receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim(); //將buffer的內容轉為UTF8編碼。

                    // 判斷訊息類型
                    if (receivedMessage.StartsWith("01")) //.StartsWith("") 用來檢查字符串是否以某個特定的字串為開頭，例如"01"。
                    {
                        // 01 開頭: 表示客戶端連接，後面的內容是暱稱
                        //Substring(2) 表示從字符串的第 2 個索引位置（即第 3 個字符 0、1、2）開始，截取剩下的所有字符，也就是去掉 "01" 的部分。
                        var nickname = receivedMessage.Substring(2);
                        客戶端列表[nickname] = client; // 將客戶端與暱稱對應起來
                        Console.WriteLine($"{nickname} 已加入伺服器。");

                        // 回覆一個確認訊息給客戶端
                        var responseMessage = "歡迎加入伺服器, " + nickname;
                        var responseBytes = Encoding.UTF8.GetBytes(responseMessage);
                        await networkStream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    }
                    else if (receivedMessage.StartsWith("02"))
                    {
                        // 02 開頭: 表示客戶端傳送的文字訊息
                        var nickname = 取得客戶端暱稱(client); // 此自訂方法會取得該客戶端的暱稱
                        var message = receivedMessage.Substring(2); // 去掉 "02" 的部分
                        Console.WriteLine($"來自 {nickname} 的訊息: {message}");

                        // 回應客戶端，確認收到訊息
                        var responseMessage = "收到訊息: " + message;
                        var responseBytes = Encoding.UTF8.GetBytes(responseMessage);
                        await networkStream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    }
                    else if (receivedMessage.StartsWith("03"))
                    {
                        // 03 開頭: 將用戶送來的資料寫入資料庫
                        var message = receivedMessage.Substring(2); // 去掉 "03" 的部分
                        string[] 用戶資料陣列 = message.Split(','); // 分割剩下的資訊
                        string 帳號 = 用戶資料陣列[0];
                        string 加密後密碼 = 加密密碼(用戶資料陣列[1]); // 使用雜湊函數加密密碼
                        string 電話 = 用戶資料陣列[2];

                        using (SQLiteConnection 連接 = new SQLiteConnection(資料庫位址字串))
                        {
                            連接.Open();
                            string sql指令 = @"INSERT INTO 測試資料表 (帳號, 密碼, 電話) 
                           VALUES (@帳號, @密碼, @電話)"; // 用參數導入的方式寫入，避免SQL注入攻擊。

                            using (SQLiteCommand 指令 = new SQLiteCommand(sql指令, 連接))
                            {
                                指令.Parameters.AddWithValue("@帳號", 帳號);
                                指令.Parameters.AddWithValue("@密碼", 加密後密碼); // 存入加密後的密碼
                                指令.Parameters.AddWithValue("@電話", 電話);

                                指令.ExecuteNonQuery();
                            }
                        }

                        Console.WriteLine($"已將加密後的資料寫入資料庫: {帳號}, [加密的密碼], {電話}");
                    }
                    else
                    {
                        Console.WriteLine("未知訊息類型: " + receivedMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }

        Console.WriteLine($"{client.Client.RemoteEndPoint} 斷線。");
        client.Close();
    }

    private static string 取得客戶端暱稱(TcpClient client) // 用來根據 TcpClient 取得暱稱
    {
        foreach (var 遍歷用變數 in 客戶端列表) //將客戶端列表(Dictionary)的key與Value傳給 遍歷用變數，並以迴圈遍歷所有客戶端列表。
        {
            if (遍歷用變數.Value == client) //當其中一個 客戶端列表的Value = 參數值client時，表示找到該對應.Key。
            {
                return 遍歷用變數.Key;
            }
        }
        return "Unknown"; //若找不到對應的.Value，則返回"Unknown"。
    }

    private static void HandleServerInput() //當伺服器管理員輸入 數字鍵1 時，可傳送自訂訊息給客戶。
    {
        while (true)
        {
            Console.WriteLine("輸入數字鍵 1 可傳送自訂訊息給客戶：");
            var input = Console.ReadLine();

            if (input == "1")
            {
                Console.WriteLine("輸入用戶端的暱稱和訊息（格式：暱稱,訊息）：");
                var 輸入的訊息值 = Console.ReadLine();
                var 分割的字串 = 輸入的訊息值.Split(','); //使用.Split(',')分割方法可以依據指定符號分割字串。

                //被分割的字串理所當然會是陣列型別，存入 分割的字串 變數，.ContainsKey()可以檢查 客戶端列表的key值是否符合 分割的字串[0]。
                if (分割的字串.Length == 2 && 客戶端列表.ContainsKey(分割的字串[0]))
                {
                    var 客戶端暱稱 = 分割的字串[0];
                    var 傳遞用訊息 = 分割的字串[1];
                    SendMessageToClient(客戶端暱稱, 傳遞用訊息);
                }
                else
                {
                    Console.WriteLine("輸入無效或未找到用戶端。");
                }
            }
        }
    }

    private static void SendMessageToClient(string nickname, string message) // 發送訊息到指定的客戶端用的自訂方法
    {
        if (客戶端列表.TryGetValue(nickname, out var 返回的客戶端值)) //用來檢查字典中是否存在指定的鍵（Key），並且在找到該鍵的情況下，同時返回其對應的值（Value）。
        {
            var networkStream = 返回的客戶端值.GetStream(); //指定某一客戶端來收發網路流，返回的客戶端值 是TcpClient物件。
            var messageBytes = Encoding.UTF8.GetBytes(message); //將UTF8編碼的string參數值，轉為Byte[] 位元組數值。
            networkStream.WriteAsync(messageBytes, 0, messageBytes.Length); //.WriteAsync()方法 將位元組陣列值發送給客戶端。
            Console.WriteLine($"訊息已傳送至 {nickname}: {message}");
        }
        else
        {
            Console.WriteLine($"Client {nickname} not found.");
        }
    }
}
