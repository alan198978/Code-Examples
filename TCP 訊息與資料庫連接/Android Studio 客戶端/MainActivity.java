package com.example.client1;
//套件宣告和匯入
import androidx.appcompat.app.AppCompatActivity;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.util.Log;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.TextView;
//用於網路操作和資料編碼, 套件宣告和匯入
import java.io.InputStream;
import java.io.OutputStream;
import java.net.Socket;
import java.nio.charset.StandardCharsets;

public class MainActivity extends AppCompatActivity {
    //以下宣告 UI
    private EditText 暱稱輸入框; //用於輸入暱稱。
    private EditText 訊息輸入框; //用於輸入訊息。
    private TextView 伺服器回應文字顯示; //用於顯示伺服器回應的訊息。
    private Button 伺服器連接按鈕; //負責連接伺服器。
    private Button 發送文字訊息按鈕; //負責發送訊息。
    private Button 發送用戶資料按鈕; //負責用戶資料。

    private Socket socket; //用於連接伺服器的 TCP 套接字。
    private String 伺服器位址 = "192.XXX.XXX.4";
    private int port = 5000;
    private OutputStream outputStream; //透過此資料流發送資料至伺服器。
    //Handler 用於更新 UI，因為網路操作通常是在子執行緒中進行，無法直接修改 UI 元素。
    private Handler handler = new Handler(Looper.getMainLooper());
    private String 暫時的用戶暱稱;
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        //以下將 XML 中的 UI 元素與代碼綁定
        暱稱輸入框 = findViewById(R.id.nicknameEditText);
        訊息輸入框 = findViewById(R.id.訊息輸入框UI);
        伺服器回應文字顯示 = findViewById(R.id.responseTextView);
        伺服器連接按鈕 = findViewById(R.id.connectButton);
        發送文字訊息按鈕 = findViewById(R.id.發送文字訊息鈕UI);
        發送用戶資料按鈕 = findViewById(R.id.發送用戶資料鈕UI);
        //以下建立按鈕監聽器
        伺服器連接按鈕.setOnClickListener(new View.OnClickListener() {
            @Override //伺服器連接按鈕 的點擊事件監聽器會觸發一個新執行緒，並執行 ConnectTask 來連接伺服器。
            public void onClick(View v) {
                String nickname = 暱稱輸入框.getText().toString(); //用nickname變數取得輸入框的輸入值
                new Thread(new ConnectTask(nickname)).start(); //new Thread 是 Java 中用來創建和啟動一個新的執行緒的方式。
                暫時的用戶暱稱 = 暱稱輸入框.getText().toString(); //發送後，暫時存入暱稱的變量。
            }
        });

        發送文字訊息按鈕.setOnClickListener(new View.OnClickListener() {
            @Override //點擊事件監聽器會發送用戶輸入的訊息給伺服器，通過新執行緒運行 sendMessageToServer 方法。
            public void onClick(View v) {
                String message = 訊息輸入框.getText().toString();
                if (outputStream != null) {
                    // 在發送之前自動加上 "02" 前綴
                    new Thread(() -> sendMessageToServer("02" + message)).start(); 
                }
            }
        });

        發送用戶資料按鈕.setOnClickListener(new View.OnClickListener() {
            @Override //點擊事件監聽器會發送用戶輸入的訊息給伺服器，通過新執行緒運行 sendMessageToServer 方法。
            public void onClick(View v) {
                String message = 暫時的用戶暱稱 + ",225566,0902XXXXXX"; //要寫入資料庫的資料(用逗號分開 暱稱,密碼,電話)
                //這邊範例上直接用一個String字串就傳送所有資料只是簡便示範，實際上請用各種輸入框來讓用戶輸入資料。
                if (outputStream != null) {
                    // 在發送之前自動加上 "03" 前綴
                    new Thread(() -> sendMessageToServer("03" + message)).start();
                }
            }
        });
    }

    private class ConnectTask implements Runnable {
        private String nickname;

        ConnectTask(String nickname) {
            this.nickname = nickname;
        }

        @Override
        public void run() {
            try {
                // 連接伺服器
                socket = new Socket(伺服器位址, port);

                // 發送暱稱，並自動加上 "01" 前綴, 透過 OutputStream 發送至伺服器。
                outputStream = socket.getOutputStream();
                outputStream.write(("01" + nickname).getBytes(StandardCharsets.UTF_8));
                outputStream.flush();

                // 更新 UI 顯示
                handler.post(() -> 伺服器回應文字顯示.append("已發送暱稱: " + nickname + "\n"));

                // 在連接後，持續監聽伺服器的訊息並更新 UI，將接收到的訊息顯示在 responseTextView 上。
                InputStream inputStream = socket.getInputStream();
                byte[] buffer = new byte[1024];
                int bytesRead;
                //while 將會持續迴圈，除非條件 = -1，才會被終止。
                while ((bytesRead = inputStream.read(buffer)) != -1) {
                    String responseMessage = new String(buffer, 0, bytesRead, StandardCharsets.UTF_8);

                    // 更新 UI 顯示伺服器回應
                    handler.post(() -> 伺服器回應文字顯示.append("來自伺服器的訊息: " + responseMessage + "\n"));
                }

            } catch (Exception e) { //紀錄與顯示錯誤
                Log.e("TCP", "Exception: " + e.getMessage());
                handler.post(() -> 伺服器回應文字顯示.append("Exception: " + e.getMessage() + "\n"));
            } finally {
                try {
                    if (socket != null && !socket.isClosed()) {
                        socket.close();
                    }
                } catch (Exception e) {
                    Log.e("TCP", "Exception during socket close: " + e.getMessage());
                }
            }
        }
    }

    //發送訊息到伺服器
    private void sendMessageToServer(String message) { //"02"或"03"前綴,已在參數中添加。
        try { //使用 OutputStream 將用戶在 messageEditText 中輸入的訊息發送至伺服器。
            if (outputStream != null) {
                outputStream.write(message.getBytes(StandardCharsets.UTF_8));
                outputStream.flush();
                //成功發送後，會在 UI 中更新，顯示已發送的訊息。
                handler.post(() -> 伺服器回應文字顯示.append("已發送訊息: " + message + "\n"));
            }
        } catch (Exception e) { //若發送過程出錯，則會捕捉異常並在 UI 上顯示錯誤訊息。
            Log.e("TCP", "Exception during sending message: " + e.getMessage());
            handler.post(() -> 伺服器回應文字顯示.append("發送訊息失敗: " + e.getMessage() + "\n"));
        }
    }
}
