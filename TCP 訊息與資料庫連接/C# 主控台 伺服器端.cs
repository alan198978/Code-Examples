using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

class Server
{
    private static TcpListener? TCP監聽;
    private static Dictionary<string, TcpClient> 客戶列表 = new Dictionary<string, TcpClient>();
    private static object lockObject = new object(); //註解*1 (請下拉到最底觀看)
    private const string 資料庫位址 = @"Data Source=C:\Users\使用者名稱\Desktop\test.db;Cache=Shared";

    static async Task Main(string[] args)
    {
        try
        {
            TCP監聽 = new TcpListener(IPAddress.Any, 5000);
            TCP監聽.Start();
            Console.WriteLine("伺服器啟動，等待連接...");

            while (true)
            {
                TcpClient client = await TCP監聽.AcceptTcpClientAsync(); //持續異步監聽，每當有客戶端連接進來時，存入client變數。
                Console.WriteLine("客戶端已連接");
                _ = 處理客戶端訊息異步函數(client); //_ = 是一種語法，表示忽略一個返回值。
                //_ = 也表示你希望啟動一個異步操作，但不需要等待它完成或處理它的結果。
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"伺服器錯誤: {ex.Message}");
        }
    }

    private static async Task 處理客戶端訊息異步函數(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] 設置的緩衝大小 = new byte[2048];
        string? 客戶端用戶名稱 = null;

        try
        {
            while (true)
            {
                int bytesRead = await stream.ReadAsync(設置的緩衝大小);
                if (bytesRead == 0)
                    break;

                string 所有訊息 = Encoding.UTF8.GetString(設置的緩衝大小, 0, bytesRead).TrimEnd('\0', '\n', '\r');

                string 訊息類別 = 所有訊息.Substring(0, 2); //用.Substring() 分割訊息，來辨識屬於哪一種訊息("02"、"03"、"04")。
                string 主要訊息 = 所有訊息.Substring(2); //分割後剩下的訊息就是主要文字訊息。
                Console.WriteLine($"\n收到訊息: {所有訊息}");

                switch (訊息類別)
                {
                    case "02": // 聊天訊息
                        if (客戶端用戶名稱 != null)
                        {
                            await 廣播訊息異步函數(客戶端用戶名稱, 主要訊息);
                        }
                        break;

                    case "03": // 註冊
                        string[] registrationData = 主要訊息.Split(',');
                        if (registrationData.Length == 3)
                        {
                            string username = registrationData[0];
                            string password = registrationData[1];
                            string phone = registrationData[2];

                            bool isDuplicate = await 檢查註冊帳號是否重複(username);
                            if (isDuplicate)
                            {
                                await 傳送訊息異步函數(stream, "帳號已存在");
                            }
                            else
                            {
                                bool success = await 註冊使用者的異步函數(username, password, phone);
                                if (success)
                                {
                                    await 傳送訊息異步函數(stream, "註冊成功");
                                }
                                else
                                {
                                    await 傳送訊息異步函數(stream, "註冊失敗");
                                }
                            }
                        }
                        break;

                    case "04": // 登入
                        string[] loginData = 主要訊息.Split(',');
                        if (loginData.Length == 2)
                        {
                            string username = loginData[0];
                            string password = loginData[1];

                            bool isValid = await 驗證使用者登入的異步函數(username, password);
                            if (isValid)
                            {                                
                                客戶端用戶名稱 = username;
                                lock (lockObject)
                                {
                                    客戶列表[客戶端用戶名稱] = client; //用戶名是key, 客戶端物件是value
                                }
                                await 傳送訊息異步函數(stream, "登入成功");
                                Console.WriteLine("客戶端登入成功");
                                await 廣播訊息異步函數(客戶端用戶名稱, "已上線");
                            }
                            else
                            {
                                await 傳送訊息異步函數(stream, "登入失敗");
                            }
                        }
                        break;

                    case "05": // 客戶端登出
                        if (客戶端用戶名稱 != null)
                        {
                            Console.WriteLine($"{客戶端用戶名稱} 已登出。");
                            await 廣播訊息異步函數(客戶端用戶名稱, "已登出");
                            lock (lockObject)
                            {
                                客戶列表.Remove(客戶端用戶名稱);
                            }
                            client.Close();
                            return; // Exit the loop without raising an error
                        }
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"處理客戶端時發生錯誤: {ex.Message}");
        }
        finally
        {
            if (客戶端用戶名稱 != null)
            {
                lock (lockObject)
                {
                    客戶列表.Remove(客戶端用戶名稱);
                }
            }
            client.Close();
            Console.WriteLine("客戶端已斷開連接");
        }
    }

    private static async Task<bool> 檢查註冊帳號是否重複(string 帳號)
    {
        using (var SQL連接 = new SqliteConnection(資料庫位址))
        {
            await SQL連接.OpenAsync(); //OpenAsync() 要對資料庫進行操作，首先必須開啟這個連接，不過它是異步類型的連接。
            var SQL命令 = SQL連接.CreateCommand(); //CreateCommand()方法是用來創建一個可用來執行SQL語法的 SqliteCommand 對象。
            SQL命令.CommandText = "SELECT COUNT(*) FROM 測試資料表 WHERE 帳號 = @Username";
            SQL命令.Parameters.AddWithValue("@Username", 帳號); //Parameters.AddWithValue 是用來添加參數到 SQL 命令的，這樣可以防止 SQL 注入並動態傳入值。

            var count = Convert.ToInt32(await SQL命令.ExecuteScalarAsync());
            return count > 0;
        }
    }

    private static async Task<bool> 驗證使用者登入的異步函數(string 帳號, string 密碼)
    {
        using (var connection = new SqliteConnection(資料庫位址))
        {
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT COUNT(*) 
            FROM 測試資料表 
            WHERE 帳號 = @Username 
            AND 密碼 = @Password 
            COLLATE NOCASE"; // 添加COLLATE NOCASE使比較不區分大小寫

            command.Parameters.AddWithValue("@Username", 帳號.Trim());
            command.Parameters.AddWithValue("@Password", 密碼.Trim());

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            if (count == 0)
            {
                Console.WriteLine($"登入失敗 - 帳號: {帳號}");
                Console.WriteLine($"提供的密碼: {密碼}");

                // 新增偵錯用查詢
                var debugCommand = connection.CreateCommand();
                debugCommand.CommandText = "SELECT 密碼 FROM 測試資料表 WHERE 帳號 = @Username";
                debugCommand.Parameters.AddWithValue("@Username", 帳號);
                var storedPassword = await debugCommand.ExecuteScalarAsync() as string;
                Console.WriteLine($"資料庫中的密碼: {storedPassword}");
            }
            return count > 0;
        }
    }

    private static async Task<bool> 註冊使用者的異步函數(string 帳號, string 密碼, string 電話)
    {
        using (var SQL連接 = new SqliteConnection(資料庫位址))
        {
            await SQL連接.OpenAsync();

            using (var transaction = SQL連接.BeginTransaction())
            {
                //.BeginTransaction() 用來開啟一個交易，交易可以確保一組資料庫操作要麼全部成功，要麼全部失敗，從而保證數據的一致性。
                //如果在註冊用戶時，某一部分操作成功，但另一部分失敗（比如帳號插入成功，但電話號碼插入失敗），
                //資料庫可能會處於不一致的狀態。而使用交易，失敗時所有操作都會回滾，確保數據一致。
                try
                {
                    var SQL命令 = SQL連接.CreateCommand(); //創建 SqliteCommand 物件。
                    SQL命令.Transaction = transaction; //將 SqliteCommand 物件與 Transaction 綁定，這意味著所有操作都會受該交易控制。
                    SQL命令.CommandText = @"
                        INSERT INTO 測試資料表 (帳號, 密碼, 電話)
                        VALUES (@Username, @Password, @Phone)";

                    SQL命令.Parameters.AddWithValue("@Username", 帳號);
                    SQL命令.Parameters.AddWithValue("@Password", 密碼);
                    SQL命令.Parameters.AddWithValue("@Phone", 電話);

                    await SQL命令.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"註冊失敗: {ex.Message}");
                    return false;
                }
            }
        }
    }

    private static async Task 傳送訊息異步函數(NetworkStream stream, string message)
    {
        try
        {
            // 在消息後添加換行符。(不知道為啥，這樣終於讓Android Studio客戶端顯示伺服器消息)
            string messageWithNewLine = message + "\n";
            byte[] messageBytes = Encoding.UTF8.GetBytes(messageWithNewLine);
            await stream.WriteAsync(messageBytes);
            await stream.FlushAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"發送訊息時發生錯誤: {ex.Message}");
        }
    }

    private static async Task 廣播訊息異步函數(string 客戶端用戶名稱, string 訊息)
    {
        List<string> 斷開連線的客戶端 = new List<string>();
        Dictionary<string, TcpClient> 客戶列表副本; //宣告一個 客戶列表 副本

        lock (lockObject)
        {
            客戶列表副本 = new Dictionary<string, TcpClient>(客戶列表); //建立一個 客戶列表 副本
            //先使用副本來操作，避免一開始就修改正本資料。
        }

        foreach (var kvp in 客戶列表副本)
        {
            try
            {
                NetworkStream stream = kvp.Value.GetStream();
                string fullMessage = $"[{客戶端用戶名稱}]: {訊息}\n";  // 添加換行符
                byte[] messageBytes = Encoding.UTF8.GetBytes(fullMessage);
                await stream.WriteAsync(messageBytes);
                await stream.FlushAsync();
            }
            catch
            {
                斷開連線的客戶端.Add(kvp.Key);
            }
        }

        if (斷開連線的客戶端.Count > 0) //移除斷線的客戶端
        {
            lock (lockObject)
            {
                //使用 lock (lockObject) 來安全地從共享的 客戶列表 中移除這些斷線的客戶端。
                foreach (string username in 斷開連線的客戶端)
                {
                    客戶列表.Remove(username);
                    //這就是為什麼要先用副本操作，在確定斷線的客戶端有哪些之後，再移除正本的客戶端。
                }
            }
        }
    }
}
//註解*1 :
//object lockObject 用防止多個執行緒同時存取和修改 客戶列表 字典。
//在此程式碼中你會看到很多 lock (lockObject){ } 區塊，
//在 lock 區塊中的程式碼會確保一次只有一個執行緒可以修改 客戶列表，從而保護它不會出現併發訪問的問題。
//因為每個客戶端連線都是由不同的任務或執行緒來處理，而這些連線可能會同時對 客戶列表 進行操作，所以有發生競爭條件（race condition）的風險。
