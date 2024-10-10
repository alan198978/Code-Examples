package com.java_api_php;

import android.os.Bundle;
import android.widget.Button;
import android.widget.TextView;
import androidx.appcompat.app.AppCompatActivity;
import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;
import retrofit2.Retrofit;
import retrofit2.converter.gson.GsonConverterFactory;
import retrofit2.http.Body;
import retrofit2.http.POST;

public class MainActivity extends AppCompatActivity {
    private TextView resultTextView; //聲明了一個 TextView變數 用於顯示 API 回應結果。

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        resultTextView = findViewById(R.id.resultTextView); //該TextView連結對應的xml布局id。
        Button sendButton = findViewById(R.id.sendButton); //聲明一個Button變數，並連結對應的xml布局id。

        sendButton.setOnClickListener(v -> 傳送資料到網頁API()); //將發送按鈕設定點擊監聽器
    }

    private void 傳送資料到網頁API() {
        Retrofit 目標網址與資料轉換 = new Retrofit.Builder() //建立一個 Retrofit.Builder 物件。
                .baseUrl("http://asdzxc.online/") //設定 API 的基礎 URL。所有的相對 端點 都將基於這個位址
                .addConverterFactory(GsonConverterFactory.create()) //新增 Gson 函式庫的轉換器，用於 JSON 和 Java/Kotlin 物件之間進行轉換。
                .build(); //使用以上配置的所有選項建立 Retrofit 實例。

        Api位置 api服務 = 目標網址與資料轉換.create(Api位置.class); //Api位置這個類別在最底下，使用它來實際發起網路請求。

        輸入資料 傳輸物件 = new 輸入資料( //傳輸物件, 是包含要傳送到伺服器的資料, 全部打包成一個物件。
                "測試商品222",
                10,
                99.99,
                "5NFkvty8mWVkiGxxoijR",
                "Nqn8mFGy3arrsdTD1eXi"
        );

        api服務.傳送資料(傳輸物件).enqueue(new Callback<Api接收回應>() { //enqueue() 最終將資料送出到API的方法, 並取得回應。
            @Override
            public void onResponse(Call<Api接收回應> call, Response<Api接收回應> response) {
                if (response.isSuccessful()) { //response.isSuccessful() 用來檢查從伺服器返回的 HTTP 回應是否成功。如果回應的狀態碼在 200 到 299 之間，這個方法會返回 true。
                    Api接收回應 apiResponse = response.body();
                    resultTextView.setText("API回應: " + apiResponse.get回應值() + ", 商品名: " + apiResponse.getMessage());
                } else {
                    resultTextView.setText("API 調用失敗: " + response.code());
                }
            }

            @Override
            public void onFailure(Call<Api接收回應> call, Throwable t) {
                resultTextView.setText("API 調用異常: " + t.getMessage());
            }
        });
    }
}

class 輸入資料 {
    public String name;
    public int quantity;
    public double price;
    public String password1;
    public String password2;
    //以下為建構函數，當class 輸入資料被呼叫時，會自動呼叫 public 輸入資料。
    public 輸入資料(String name, int quantity, double price, String password1, String password2) {
        this.name = name;
        this.quantity = quantity;
        this.price = price;
        this.password1 = password1;
        this.password2 = password2;
    }
}

class Api接收回應 {
    private String 回應值;
    private String message;

    public String get回應值() {
        return 回應值;
    }

    public void set回應值(String 回應值) {
        this.回應值 = 回應值;
    } //本範例中未使用到set。

    public String getMessage() {
        return message;
    }

    public void setMessage(String message) {
        this.message = message;
    } //本範例中未使用到set。
}

interface Api位置 { //用來定義 Retrofit 的 API 端點
    @POST("接收資料.php") //這個方法將會對 /接收資料.php 路徑發起一個 HTTP POST 請求。
    Call<Api接收回應> 傳送資料(@Body 輸入資料 inputData); //返回的 Call<Api接收回應> 物件允許你處理 API 回應
}