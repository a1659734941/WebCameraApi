@echo off
chcp 65001 >nul
setlocal

set SERVICE_NAME=WebCameraApi

echo ========================================
echo    WebCameraApi 删除服务
echo ========================================
echo 服务名称: %SERVICE_NAME%
echo.

sc query "%SERVICE_NAME%" >nul 2>&1
if errorlevel 1 (
    echo [提示] 服务不存在或未注册
    pause
    exit /b 0
)

echo [进行中] 正在停止服务...
sc stop "%SERVICE_NAME%" >nul 2>&1
timeout /t 2 >nul

echo [进行中] 正在删除服务...
sc delete "%SERVICE_NAME%"
if errorlevel 1 (
    echo [失败] 删除服务失败
    echo 可能原因: 需要管理员权限
    pause
    exit /b 1
)

echo [成功] 服务已删除
pause
