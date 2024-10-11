<?php
$db_file = 'C:\\Users\\使用者名稱\\Desktop\\test.db'; //電腦中資料庫檔案位置
$db = new PDO('sqlite:' . $db_file);
$db->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
?>
