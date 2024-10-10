<?php
ini_set('display_errors', 1);
ini_set('display_startup_errors', 1);
error_reporting(E_ALL);

$db_file = 'C:\\Users\\使用者資料夾\\Desktop\\test.db'; //電腦中資料庫檔案位置
$db = new PDO('sqlite:' . $db_file);
$db->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION); //實際應使用include 'xxx.php';將資料庫敏感訊息分開

// 產生唯一識別碼
$unique_id = bin2hex(random_bytes(16));

// 先做一次插入資料到資料庫 (此處僅為範例, 實際應在用戶確認購物清單, 準備結帳之前就寫入資料庫。)
$order_id = 'NL10001';
$user_name = '用戶A';
$product_name = 'Product X';
$product_price = '100';
$paid = false;

$stmt = $db->prepare("INSERT INTO 測試資料表 (訂單編號, 用戶姓名, 商品名稱, 訂單總價, 已付款) VALUES (?, ?, ?, ?, ?)");
$stmt->execute([$order_id, $user_name, $product_name, $product_price, $paid]);

// 創建一個關聯數組包含商品信息和賣家認證
$seller_data = json_encode([
    'order_id' => $order_id,
    'user_name' => $user_name,
    'product_name' => $product_name,
    'product_price' => $product_price,
    'seller_password' => 'SellerSecretPassword123',//模擬賣家用戶的固定密碼。
    'unique_id' => $unique_id //唯一識別碼，每次隨機產生。
]);

$payment_url = 'http://網址/接收方.php';

$ch = curl_init($payment_url);
curl_setopt($ch, CURLOPT_POST, true);
curl_setopt($ch, CURLOPT_POSTFIELDS, $seller_data);
curl_setopt($ch, CURLOPT_HTTPHEADER, array('Content-Type: application/json'));
curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);

$response = curl_exec($ch);

if ($response === false) {
    echo 'Error: ' . curl_error($ch);
} else {
    $http_code = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    echo 'HTTP Response Code: ' . $http_code . "</br>";

    $response_data = json_decode($response, true);
    if (isset($response_data['status']) && isset($response_data['message'])) { //先判斷是否存在 $response_data[]
        echo $response_data['status'] . ', ' . $response_data['message']; //網頁顯示 成功, 付款已成功收到
        if ($response_data['status'] === "成功") { //若['status'] 等於 "成功"
            //先判斷 唯一識別碼 和 訂單編號是否正確。
            if( $response_data['unique_id'] === $unique_id && $response_data['order_id'] === $order_id)
            {
                $stmt = $db->prepare("UPDATE 測試資料表 SET 已付款 = ? WHERE 訂單編號 = ?");
                $stmt->execute([true, $order_id]); //更新資料庫中的已付款狀態。
            }
        }
    } else {
        echo '無效的回應格式';
    }
}

curl_close($ch);
?>
