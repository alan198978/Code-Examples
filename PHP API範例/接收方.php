<?php
ini_set('display_errors', 1);
ini_set('display_startup_errors', 1);
error_reporting(E_ALL); //啟用了錯誤顯示和報告，對於開發階段的調試很有幫助。

if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    echo json_encode(['status' => 'error', 'message' => '無效的請求方法']);
    exit; //檢查請求是否為 POST 方法，如果不是，返回 405 狀態碼。
}

$input_data = file_get_contents('php://input'); //從請求體讀取原始數據
$data = json_decode($input_data, true); //將 JSON 數據轉換為 PHP 數組

if (json_last_error() !== JSON_ERROR_NONE) {
    http_response_code(400);
    echo json_encode(['status' => 'error', 'message' => '無效的 JSON 格式']);
    exit; //如果 JSON 解析失敗，返回 400 狀態碼（錯誤請求）
}

if (!isset($data['order_id'], $data['user_name'], $data['product_name'], $data['product_price'], $data['seller_password'], $data['unique_id'])) {
    http_response_code(400);
    echo json_encode(['status' => 'error', 'message' => '缺少必要欄位']);
    exit; //檢查是否包含所有必需的欄位
}

if ($data['seller_password'] !== 'SellerSecretPassword123') {
    http_response_code(403);
    echo json_encode(['status' => 'error', 'message' => '無效的賣家密碼']);
    exit; //檢查提供的賣家密碼是否正確，如果不正確返回 403 狀態碼
}

http_response_code(200);
echo json_encode(['status' => '成功', 'message' => '付款已成功收到', 'order_id' => $data['order_id'], 'unique_id' => $data['unique_id']]);
//如果所有驗證都通過，返回成功響應，包括：
//200 狀態碼（成功）
//JSON 格式的響應，包含狀態、消息、訂單ID和唯一識別碼
?>
