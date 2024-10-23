using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

class Client
{
    private static TcpClient? client;
    private static NetworkStream? stream;
    private static string? 當前用戶名;
    private static bool 已登入 = false;
    private static Task? 持續接收訊息任務;
    private static bool 等待登入回應 = false;
    private static bool 已退出 = false;

    static async Task Main(string[] args)
    {
        try
        {//124.218.45.119
            client = new TcpClient("127.0.0.1", 5000);
            stream = client.GetStream();

            持續接收訊息任務 = 異步接收訊息函數(); //這個變數會異步持續監聽與接收來自伺服器的訊息。

            while (!已退出)
            {
                if (!已登入)
                {
                    await 異步顯示登入選單();
                }
                else
                {
                    await 異步顯示發送訊息選單();
                }

                while (等待登入回應)
                {
                    //await Task.Delay(100) 是一個異步暫停方法，表示暫停100毫秒。
                    await Task.Delay(100);
                }
            }
        }
        catch (Exception ex)
        {
            if (!已退出)
            {
                Console.WriteLine($"錯誤: {ex.Message}");
            }
        }
        finally
        {
            client?.Close();
        }
    }
    private static string 密碼加密函數(string input)
    {
		//由客戶端進行加密，以避免明碼傳送資料。
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

    private static async Task 異步顯示登入選單()
    {
        Console.WriteLine("\n=== 主選單 ===");
        Console.WriteLine("1. 註冊新帳號");
        Console.WriteLine("2. 登入系統");
        Console.WriteLine("3. 退出");
        Console.Write("請選擇功能 (1-3): ");

        string? choice = Console.ReadLine();
        switch (choice)
        {
            case "1":
                await 異步註冊帳戶函數();
                break;
            case "2":
                await 異步登入系統函數();
                break;
            case "3":
                await 異步退出函數();
                break;
            default:
                Console.WriteLine("無效的選擇，請重試。");
                break;
        }
    }

    private static async Task 異步顯示發送訊息選單()
    {
        Console.WriteLine("\n=== 主選單 ===");
        Console.WriteLine($"當前登入帳號: {當前用戶名}");
        Console.WriteLine("1. 發送訊息");
        Console.WriteLine("2. 退出");
        Console.Write("請選擇功能 (1-2): ");

        string? choice = Console.ReadLine();
        switch (choice)
        {
            case "1":
                await 異步發送自行輸入訊息函數();
                break;
            case "2":
                await 異步退出函數();
                break;
            default:
                Console.WriteLine("無效的選擇，請重試。");
                break;
        }
    }
    private static async Task 異步退出函數()
    {
        if (client != null && stream != null)
        {
            已登入 = false;
            已退出 = true;
            await 異步傳送訊息函數("05");            
        }
        stream?.Close();
        client?.Close();
        stream = null;
        client = null;
        Console.WriteLine("已退出並斷開連接");

        Environment.Exit(0);  //當這行代碼被執行時，它會立即結束應用程序，並將一個整數狀態碼傳遞給操作系統。
    }

    private static async Task 異步傳送訊息函數(string message)
    {
        if (stream != null)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(messageBytes); //用於將指定的字節數組，異步寫入到網絡流中。
            await stream.FlushAsync(); //FlushAsync 可以確保所有已寫入的數據被立即發送出去，而不是等到緩衝區滿了才自動發送。
        }
    }

    private static async Task 異步接收訊息函數()
    {
        byte[] buffer = new byte[1024];

        while (true)
        {
            try
            {
                if (stream == null || 已退出) break;

                int bytesRead = await stream.ReadAsync(buffer); //從網絡流中異步讀取數據，並將讀取的字節數量存儲在 bytesRead 變數中。
                if (bytesRead == 0) break;  // 若字節數量 = 0，表示沒有讀取到任何數據。

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (message.StartsWith("登入成功"))
                {
                    已登入 = true;
                    等待登入回應 = false;
                    Console.WriteLine("登入成功！");
                }
                else if (message.StartsWith("["))
                {
                    // 處理聊天訊息
                    Console.WriteLine($"\n ☆ 收到訊息: {message}");
                }
                else
                {
                    // 處理系統訊息
                    Console.WriteLine($"系統訊息: {message}");
                }
            }
            catch (Exception ex)
            {
                if (!已退出)
                {
                    Console.WriteLine($"接收訊息時發生錯誤: {ex.Message}");
                }
                break;
            }
        }
    }
    private static async Task 異步註冊帳戶函數()
    {
        Console.Write("請輸入帳號: ");
        string? username = Console.ReadLine();
        Console.Write("請輸入密碼: ");
        string? password = Console.ReadLine();
        Console.Write("請輸入電話號碼: ");
        string? phone = Console.ReadLine();

        string 雜湊密碼 = 密碼加密函數(password);
        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(雜湊密碼) && !string.IsNullOrEmpty(phone))
        {
            string 加入辨識碼的訊息 = $"03{username},{雜湊密碼},{phone}";
            await 異步傳送訊息函數(加入辨識碼的訊息);
        }
        else
        {
            Console.WriteLine("註冊資訊無效，請重試。");
        }
    }

    private static async Task 異步登入系統函數()
    {
        Console.Write("請輸入帳號: ");
        string? username = Console.ReadLine();
        Console.Write("請輸入密碼: ");
        string? password = Console.ReadLine();

        string 雜湊密碼 = 密碼加密函數(password);

        //string.IsNullOrEmpty() 用來檢查變數是否為 null，是 null 則返回true。但前面若加了 ! 則表示為:不是 null 返回true。
        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(雜湊密碼))
        {
            等待登入回應 = true;
            string 加入辨識碼的訊息 = $"04{username},{雜湊密碼}";
            await 異步傳送訊息函數(加入辨識碼的訊息);
            當前用戶名 = username;
        }
        else
        {
            Console.WriteLine("登入資訊無效，請重試。");
        }
    }

    private static async Task 異步發送自行輸入訊息函數()
    {
        Console.Write("輸入訊息: ");
        string? 輸入的訊息 = Console.ReadLine();

        if (!string.IsNullOrEmpty(輸入的訊息))
        {
            string 加入辨識碼的訊息 = $"02{輸入的訊息}";
            await 異步傳送訊息函數(加入辨識碼的訊息);
        }
        else
        {
            Console.WriteLine("訊息不能為空！");
        }
    }
}