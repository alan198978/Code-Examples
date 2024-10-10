<?php
ini_set('display_errors', 1);
ini_set('display_startup_errors', 1);
error_reporting(E_ALL);

if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    echo json_encode(['status' => 'error', 'message' => '無效的請求方法']);
    exit;
}

$input_data = file_get_contents('php://input');
$data = json_decode($input_data, true);

if (json_last_error() !== JSON_ERROR_NONE) {
    http_response_code(400);
    echo json_encode(['status' => 'error', 'message' => '無效的 JSON 格式']);
    exit;
}

if (!isset($data['order_id'], $data['user_name'], $data['product_name'], $data['product_price'], $data['seller_password'], $data['unique_id'])) {
    http_response_code(400);
    echo json_encode(['status' => 'error', 'message' => '缺少必要欄位']);
    exit;
}

if ($data['seller_password'] !== 'SellerSecretPassword123') {
    http_response_code(403);
    echo json_encode(['status' => 'error', 'message' => '無效的賣家密碼']);
    exit;
}

http_response_code(200);
echo json_encode(['status' => '成功', 'message' => '付款已成功收到', 'order_id' => $data['order_id'], 'unique_id' => $data['unique_id']]);
?>

