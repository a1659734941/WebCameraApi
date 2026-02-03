# WebCameraApi

## 项目简介

WebCameraApi 是一个基于 .NET 8.0 的 Web API 项目，用于集成和管理海康威视摄像头、门禁设备和报警系统。该项目提供了统一的接口来获取摄像头 RTSP 地址、控制门禁开关、接收和处理报警数据等功能。

## 主要功能

### 1. 摄像头 RTSP 地址获取
- 通过摄像头名称获取 RTSP 地址
- 支持 GET 和 POST 两种请求方式
- 从 PostgreSQL 数据库读取摄像头配置
- 使用 ONVIF 协议获取 RTSP 地址
- 自动拼接完整的 RTSP 地址（包含认证信息）

### 2. 海康门禁控制
- **开门**：通过门禁名称或 IP 控制门禁开门，前端直接传入门禁信息（IP、端口、账号、密码）
- **人脸录制**：从门禁设备采集人脸并保存到 `wwwroot/RecordFaceImage`，返回 Base64 与相对路径
- **人员管理**：
  - **下发人员**（addUser）：向一台或多台门禁下发人员信息（工号、姓名、有效期等）
  - **查询人员**（queryUsers）：按工号/姓名查询门禁上的人员列表，支持分页
  - **下发人脸**（addFace）：向门禁下发人脸并绑定到已有人员（Base64 或服务器路径）
  - **删除用户**（deleteUser）：按工号删除门禁用户（同时删除关联的卡、指纹、人脸）；删除命令通过 HTTP ISAPI（80 端口）下发，删除进度通过 SDK 长连接（8000 端口）轮询
- 使用海康威视 SDK（登录、开门、查询/下发/删除人员及进度）与 HTTP ISAPI（删除命令 PUT）配合
- 支持多台门禁设备批量操作（开门、下发人员、查询、下发人脸、删除用户）

### 3. 海康报警/计数服务
- **人数计数接口**：接收摄像头人数计数数据，当人数少于阈值时自动发送锁定/解锁请求
- **行为分析接口**：接收摄像头行为分析 JSON 数据，包括制服检测、人数统计、超时逗留、起立检测、攀高检测等
- **报警记录查询**：支持按时间范围、事件类型、设备名称筛选，支持分页查询
- **报警记录统计**：获取所有报警记录中不同事件类型出现的次数统计
- **图片下载和存储**：自动下载报警截图并保存到本地，返回相对路径供前端访问
- **防抖机制**：避免同一 IP 短时间内重复发送相同请求（默认1000ms）
- **高并发支持**：使用并发安全字典（ConcurrentDictionary）处理多线程场景

## 技术栈

- **框架**：.NET 8.0
- **数据库**：PostgreSQL
- **日志**：Serilog（支持控制台和文件输出，按天滚动保留180天）
- **API 文档**：Swagger
- **协议支持**：ONVIF（摄像头）、海康威视 SDK（门禁和报警）
- **HTTP 客户端**：HttpClient（支持超时控制和重定向配置）
- **JSON 处理**：Newtonsoft.Json、System.Text.Json
- **数据库驱动**：Npgsql
- **编码支持**：System.Text.Encoding.CodePages
- **ONVIF 库**：XiaoFeng.Onvif

## 环境要求

- **操作系统**：Windows x64
- **.NET SDK**：.NET 8.0 或更高版本
- **数据库**：PostgreSQL 10.0 或更高版本
- **依赖库**：海康威视 SDK 文件（已包含在 `res/lib` 目录）
- **网络要求**：
  - 摄像头设备需支持 ONVIF 协议
  - 门禁设备需支持海康威视 SDK
  - 需要访问摄像头和门禁设备的网络连通性

## 安装和配置

### 1. 克隆项目

```bash
git clone http://192.168.8.111/taiyang0217/WebApi-FazhiScreen.git
cd WebCameraApi
```

### 2. 配置数据库连接

编辑 `appsettings.json` 文件，配置 PostgreSQL 数据库连接信息：

```json
{
  "PostresSQLConfig": {
    "host": "your_database_host",
    "database": "your_database_name",
    "username": "your_username",
    "password": "your_password",
    "port": 5432
  }
}
```

### 3. 生产环境配置

编辑 `appsettings.Production.json` 文件，配置生产环境的 URL 和数据库连接：

```json
{
  "Urls": "http://0.0.0.0:12345;https://0.0.0.0:54321",
  "PostresSQLConfig": {
    "host": "production_database_host",
    "database": "production_database_name",
    "username": "production_username",
    "password": "production_password",
    "port": 54321
  }
}
```

### 4. 安装依赖

```bash
dotnet restore
```

### 5. 构建项目

```bash
dotnet build
```

### 6. 运行项目

开发环境：

```bash
dotnet run
```

生产环境：

```bash
dotnet run --environment Production
```

或使用提供的打包脚本：

```bash
打包.bat
```

### 7. 海康摄像头推送配置（关键）

本项目包含两个海康推送接口：**人数计数**（摄像头推送）与 **行为分析**（摄像头/分析服务器推送）。下面步骤必须逐条完成，确保请求能被系统识别并写入数据库。

#### 7.1 准备 API 服务地址

1. **确定服务监听地址**
   - 开发环境：`Properties/launchSettings.json` 中 `applicationUrl`（默认 `http://localhost:5283`）
   - 生产环境：`appsettings.Production.json` 中 `Urls`（默认 `http://0.0.0.0:12345`）
2. **确认设备能访问 API**
   - 摄像头或行为分析服务器必须能访问 `http://{API服务器IP}:{端口}`
   - 若有防火墙/安全组，需放行 API 端口（默认 5283 或 12345）

#### 7.2 配置摄像头人数计数推送（CountingCamera）

1. **登录摄像头 Web 管理界面**
2. 进入「事件配置 / 人数统计 / 事件联动 / HTTP 通知」（不同型号名称略有差异）
3. **启用 HTTP 推送**
4. **填写推送地址**
   - `http://{API服务器IP}:{端口}/api/HikAlarm/CountingCamera`
5. **请求方法与格式**
   - 方法：`POST`
   - 内容类型：`application/x-www-form-urlencoded` 或 `multipart/form-data`
6. **字段名必须是** `personQueueCounting`
7. **JSON 结构必须包含以下层级**
```json
{
  "RegionCapture": {
    "humanCounting": {
      "count": 2
    }
  }
}
```
8. **绑定关系必须存在**
   - 系统通过请求源 IP（Remote IP）匹配 `hik_alarm_bind.AlarmCameraIP`
   - 若摄像头经由代理/NAT 转发，Remote IP 会变化，需确保数据库记录与实际来源 IP 一致

#### 7.3 配置行为分析推送（BehaviorAnalysisJson）

1. **登录摄像头或行为分析服务器的配置界面**
2. 进入「行为分析 / 事件联动 / HTTP 通知」
3. **启用 HTTP 推送**
4. **填写推送地址**
   - `http://{API服务器IP}:{端口}/api/HikAlarm/BehaviorAnalysisJson`
5. **请求方法与格式**
   - 方法：`POST`
   - 内容类型：`application/json`
6. **事件类型（eventType）**
   - `uniformDetection`（制服检测）
   - `peopleNumCounting`（人数统计）
   - `overtimeTarry`（超时逗留）
   - `standUp`（起立检测）
   - `advReachHeight`（攀高检测）
7. **JSON 结构必须包含**
```json
{
  "eventType": "uniformDetection",
  "dateTime": "2026-01-24T10:30:00",
  "targetAttrs": {
    "deviceName": "摄像头1",
    "channelName": "通道1",
    "taskname": "制服检测任务"
  },
  "UniformDetection": {
    "BackgroundImage": {
      "resourcesContent": "http://camera-ip/path/to/image.jpg"
    }
  }
}
```
**注意**：`eventType` 的值必须与对象名大小写对应（例如 `uniformDetection` 对应 `UniformDetection`）。

#### 7.4 绑定配置与联动目标

1. **绑定表 `hik_alarm_bind` 必须有对应记录**
2. `AlarmCameraIP` = 摄像头实际推送请求的来源 IP
3. `BlockComputerIP` = 需要接收锁定/解锁的电脑 IP
4. 目标电脑需开放 `7410` 端口，并提供 `/blockinput` 接口

#### 7.5 网络与防火墙检查

1. 摄像头/分析服务器 → API 服务器：开放 API 端口
2. API 服务器 → 目标电脑：开放 `7410` 端口
3. API 服务器 → 摄像头图片地址：允许下载报警图片

## API 接口文档

### 通用说明

- **Base URL**：`http://{服务器IP}:{端口}`（开发默认 `http://localhost:5283`，生产见 `appsettings.Production.json` 的 `Urls`）
- **通用响应格式**：所有接口返回 JSON，结构为 `{ "code": 200, "msg": "提示信息", "data": ... }`。成功时 `code` 为 200，失败时 `code` 为 400/500 等，`msg` 为错误说明，`data` 为业务数据（失败时可为 null）
- **Content-Type**：请求体为 JSON 时使用 `Content-Type: application/json`
- **Swagger**：启动后访问 `/swagger` 可查看并调试全部接口

---

### 1. 摄像头 RTSP 接口（api/CameraRtsp）

#### 1.1 获取 RTSP 地址（GET）

| 项目 | 说明 |
|------|------|
| **接口地址** | `GET /api/CameraRtsp/GetRtsp` |
| **请求参数** | `cameraName`（Query，必填）：摄像头名称 |
| **成功 code** | 200 |
| **失败 code** | 400（如摄像头名称为空、未找到配置） |

**请求示例**：
```
GET /api/CameraRtsp/GetRtsp?cameraName=摄像头1
```

**响应示例**：
```json
{
  "code": 200,
  "msg": "获取RTSP地址成功",
  "data": {
    "cameraName": "摄像头1",
    "rtspUrl": "rtsp://username:password@192.168.1.100:554/Streaming/Channels/101"
  }
}
```

#### 1.2 获取 RTSP 地址（POST）

| 项目 | 说明 |
|------|------|
| **接口地址** | `POST /api/CameraRtsp/GetRtsp` |
| **请求体** | JSON 字符串，内容为摄像头名称，例如 `"摄像头1"` |
| **Content-Type** | `application/json` |

**请求示例**：
```
POST /api/CameraRtsp/GetRtsp
Content-Type: application/json

"摄像头1"
```

**响应格式**：与 GET 相同，`data` 含 `cameraName`、`rtspUrl`。

#### 1.3 批量添加/更新摄像头配置（POST）

| 项目 | 说明 |
|------|------|
| **接口地址** | `POST /api/CameraRtsp/BatchAddConfig` |
| **请求体** | JSON 数组，每项为一条摄像头配置 |
| **说明** | 以 `CameraName` 作为主键，已存在则更新；返回成功/失败条数及错误明细 |

**请求参数说明**（每项）：

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| CameraName | string | 是 | 摄像头名称 |
| CameraIP | string | 是 | IP 地址 |
| CameraUser | string | 是 | 用户名 |
| CameraPassword | string | 是 | 密码 |
| CameraPort | int | 是 | 端口，须 > 0 |
| CameraRetryCount | int | 否 | 重试次数，默认 3 |
| CameraWaitmillisecounds | int | 否 | 等待毫秒数，默认 1000 |

**请求示例**：
```json
[
  {
    "CameraName": "审讯室1",
    "CameraIP": "192.168.1.101",
    "CameraUser": "admin",
    "CameraPassword": "password",
    "CameraPort": 80,
    "CameraRetryCount": 3,
    "CameraWaitmillisecounds": 1000
  }
]
```

**响应示例**：
```json
{
  "code": 200,
  "msg": "批量摄像头配置处理完成",
  "data": {
    "total": 2,
    "success": 2,
    "failed": 0,
    "errors": []
  }
}
```

### 2. 门禁控制接口（api/HikAC）

门禁请求中设备项 `devices[].*` 通用字段：`hikAcIP`、`hikAcPort`（默认 8000）、`hikAcUserName`、`hikAcPassword`、`acName`（门禁名称，可选）。

#### 2.1 开启门禁

| 项目 | 说明 |
|------|------|
| **接口地址** | `POST /api/HikAC/openHikAC` |
| **请求体** | 单台门禁：`HikAcIP`、`HikAcPort`、`HikAcUserName`、`HikAcPassword`、`AcName` |

**请求示例**：
```json
{
  "HikAcIP": "192.168.1.200",
  "HikAcPort": 8000,
  "HikAcUserName": "admin",
  "HikAcPassword": "password",
  "AcName": "门禁1"
}
```

**响应示例**：
```json
{
  "code": 200,
  "msg": "门禁 : 门禁1开门成功!",
  "data": {
    "acName": "门禁1"
  }
}
```

#### 2.2 门禁人脸录制

| 项目 | 说明 |
|------|------|
| **接口地址** | `POST /api/HikAC/recordFace` |
| **请求体** | 单台门禁连接参数（同上），无 `devices` 数组 |
| **功能** | 从门禁设备采集人脸，保存到 `wwwroot/RecordFaceImage`，返回 Base64 与相对路径 |

**请求示例**：
```json
{
  "HikAcIP": "192.168.1.108",
  "HikAcPort": 8000,
  "HikAcUserName": "admin",
  "HikAcPassword": "password",
  "AcName": "门禁1"
}
```

**响应示例**：
```json
{
  "code": 200,
  "msg": "门禁 : 门禁1人脸录制成功!",
  "data": {
    "acName": "门禁1",
    "faceImageBase64": "base64编码的人脸图片...",
    "faceImageFileName": "FaceData_xxx.jpg",
    "faceImageRelativePath": "RecordFaceImage/FaceData_xxx.jpg"
  }
}
```

#### 2.3 下发人员信息（新增）

| 项目 | 说明 |
|------|------|
| **接口地址** | `POST /api/HikAC/addUser` |
| **请求体** | `devices`（门禁列表）、`userID`（工号）、`userName`（姓名）、`startTime`/`endTime`（有效期，可选） |
| **功能** | 向一台或多台门禁下发人员信息，支持批量设备 |

**请求示例**：
```json
{
  "devices": [
    {
      "hikAcIP": "192.168.1.108",
      "hikAcPort": 8000,
      "hikAcUserName": "admin",
      "hikAcPassword": "password",
      "acName": "A门禁"
    }
  ],
  "userID": "1001",
  "userName": "张三",
  "startTime": "2026-01-01 00:00:00",
  "endTime": "2026-12-31 23:59:59"
}
```

**响应示例**：
```json
{
  "code": 200,
  "msg": "下发人员成功",
  "data": {
    "userID": "1001",
    "userName": "张三",
    "results": [
      {
        "hikAcIP": "192.168.1.108",
        "hikAcPort": 8000,
        "acName": "A门禁",
        "isSuccess": true,
        "message": "下发人员成功",
        "deviceResponse": ""
      }
    ]
  }
}
```

#### 2.4 查询人员信息

| 项目 | 说明 |
|------|------|
| **接口地址** | `POST /api/HikAC/queryUsers` |
| **请求体** | `devices`（门禁列表）、`userID`（可选）、`userName`（可选） |
| **功能** | 按工号/姓名（可选）查询门禁上的人员列表，支持分页 |

**请求示例**：
```json
{
  "devices": [
    {
      "hikAcIP": "192.168.1.108",
      "hikAcPort": 8000,
      "hikAcUserName": "admin",
      "hikAcPassword": "password",
      "acName": "A门禁"
    }
  ],
  "userID": "1001",
  "userName": "张三"
}
```

**响应示例**：
```json
{
  "code": 200,
  "msg": "查询人员成功",
  "data": {
    "results": [
      {
        "hikAcIP": "192.168.1.108",
        "hikAcPort": 8000,
        "acName": "A门禁",
        "totalMatches": 1,
        "users": [
          {
            "userID": "1001",
            "userName": "张三",
            "userType": "normal",
            "validEnabled": true,
            "beginTime": "2026-01-01T00:00:00",
            "endTime": "2026-12-31T23:59:59"
          }
        ],
        "isSuccess": true,
        "message": "查询成功"
      }
    ]
  }
}
```

#### 2.5 下发人脸到门禁

| 项目 | 说明 |
|------|------|
| **接口地址** | `POST /api/HikAC/addFace` |
| **请求体** | `devices`、`employeeNo`（工号，必填）、`name`、`faceImageBase64` 或 `faceImagePath`（二选一）、`FDID`（人脸库 ID，默认 1） |
| **功能** | 向门禁下发人脸并绑定到已有人员 |

**请求示例**：
```json
{
  "devices": [
    {
      "hikAcIP": "192.168.1.108",
      "hikAcPort": 8000,
      "hikAcUserName": "admin",
      "hikAcPassword": "password",
      "acName": "A门禁"
    }
  ],
  "employeeNo": "1001",
  "name": "张三",
  "faceImageBase64": "base64编码的人脸图片...",
  "FDID": "1"
}
```

**响应示例**：
```json
{
  "code": 200,
  "msg": "下发人脸成功",
  "data": {
    "employeeNo": "1001",
    "name": "张三",
    "results": [
      {
        "hikAcIP": "192.168.1.108",
        "hikAcPort": 8000,
        "acName": "A门禁",
        "isSuccess": true,
        "message": "下发人脸成功",
        "deviceResponse": ""
      }
    ]
  }
}
```

#### 2.6 删除门禁用户信息

| 项目 | 说明 |
|------|------|
| **接口地址** | `POST /api/HikAC/deleteUser` |
| **请求体** | `devices`、`userID`（要删除的人员工号，必填） |
| **功能** | 按工号删除门禁用户；同时删除其关联的卡、指纹、人脸。删除命令经 HTTP ISAPI（80 端口）下发，进度经 SDK（8000 端口）轮询 |

**请求示例**：
```json
{
  "devices": [
    {
      "hikAcIP": "192.168.1.108",
      "hikAcPort": 8000,
      "hikAcUserName": "admin",
      "hikAcPassword": "password",
      "acName": "A门禁"
    }
  ],
  "userID": "0217"
}
```

**响应示例**：
```json
{
  "code": 200,
  "msg": "删除用户成功",
  "data": {
    "userID": "0217",
    "results": [
      {
        "hikAcIP": "192.168.1.108",
        "hikAcPort": 8000,
        "acName": "A门禁",
        "isSuccess": true,
        "message": "删除用户成功",
        "deviceResponse": ""
      }
    ]
  }
}
```

### 3. 报警/计数接口

#### 人数计数接口

**接口地址**：`/api/HikAlarm/CountingCamera`

**请求方法**：POST

**请求方式**：表单数据

**请求参数**：
- `personQueueCounting`：人数计数 JSON 数据

**功能说明**：
- 接收摄像头发送的人数计数数据
- 当人数少于 3 时，自动向绑定的计算机发送锁定请求
- 当人数大于等于 3 时，发送解锁请求
- 支持防抖机制，避免短时间内重复发送相同请求

**响应示例**：
```json
{
  "code": 200,
  "message": "处理成功"
}
```

#### 行为分析接口

**接口地址**：`/api/HikAlarm/BehaviorAnalysisJson`

**请求方法**：POST

**请求方式**：JSON

**请求参数**：
- `eventType`：事件类型（uniformDetection、peopleNumCounting、overtimeTarry、standUp、advReachHeight）
- `dateTime`：事件时间
- `targetAttrs`：目标属性（包含设备名称、通道名称等）

**功能说明**：
- 接收摄像头发送的行为分析 JSON 数据
- 自动下载报警截图并保存到本地
- 将报警记录存储到数据库
- 支持多种事件类型：制服检测、人数统计、超时逗留、起立检测、攀高检测

**响应示例**：
```json
{
  "code": 200,
  "message": "行为分析数据处理成功"
}
```

#### 查询报警记录

**接口地址**：`/api/HikAlarm/SelectAlarmInfomation`

**请求方法**：GET

**请求参数**：
- `startTime`（可选）：开始时间（格式：yyyy-MM-dd HH:mm:ss）
- `endTime`（可选）：结束时间（格式：yyyy-MM-dd HH:mm:ss）
- `eventType`（可选）：报警类型
- `deviceName`（可选）：设备名称
- `pageNumber`（可选）：页码，默认 1
- `pageSize`（可选）：页大小，默认 15

**请求示例**：
```
GET /api/HikAlarm/SelectAlarmInfomation?startTime=2026-01-01 00:00:00&endTime=2026-01-31 23:59:59&eventType=制服检测&pageNumber=1&pageSize=15
```

**响应示例**：
```json
{
  "code": 200,
  "message": "查询成功",
  "data": {
    "记录ID1": {
      "id": "记录ID1",
      "eventType": "制服检测",
      "eventTime": "2026-01-21T10:30:00",
      "deviceName": "摄像头1",
      "channelName": "通道1",
      "taskName": "制服检测任务",
      "snapshotBase64Path": "HikAlarmSnapshotBase64/摄像头1_制服检测_20260121103000123.jpg",
      "rawData": "原始JSON数据"
    }
  }
}
```

#### 获取最近 6 个月报警统计

**接口地址**：`/api/HikAlarm/GetRecentSixMonthAlarmStats`

**请求方法**：GET

**请求参数**：无

**功能说明**：
- 按自然月汇总最近 6 个月的报警数量
- 返回月份列表（格式：`yyyy年M月`）与对应的报警数量列表（一一对应）
- 用于趋势图、月度报表等展示

**响应示例**：
```json
{
  "code": 200,
  "msg": "请求成功",
  "data": {
    "dateList": ["2025年9月", "2025年10月", "2025年11月", "2025年12月", "2026年1月", "2026年2月"],
    "alarmTotalList": [12, 8, 15, 20, 6, 3]
  }
}
```

#### 获取所有报警记录统计

**接口地址**：`/api/HikAlarm/GetAllAlarmRecordCount`

**请求方法**：GET

**功能说明**：
- 获取所有报警记录中不同事件类型出现的次数
- 自动将事件类型翻译为中文
- 用于统计分析和报表展示

**响应示例**：
```json
{
  "code": 200,
  "message": "获取成功",
  "data": [
    { "value": 10, "name": "制服检测" },
    { "value": 5, "name": "人数统计" },
    { "value": 3, "name": "超时逗留" },
    { "value": 2, "name": "起立检测" },
    { "value": 1, "name": "攀高检测" }
  ]
}
```

## 数据库表结构

### 摄像头配置表（onvif_camera_infomation）

| 字段名 | 类型 | 说明 |
|--------|------|------|
| id | UUID | 主键 |
| camera_name | VARCHAR | 摄像头名称 |
| ip | VARCHAR | IP 地址 |
| port | INTEGER | 端口号 |
| user | VARCHAR | 用户名 |
| password | VARCHAR | 密码 |
| retry_count | INTEGER | 重试次数 |
| wait_milliseconds | INTEGER | 等待毫秒数 |

### 门禁配置（不落库）

门禁信息不保存，前端每次开门时直接传入门禁 IP、端口、用户名、密码、名称。

### 报警绑定配置表（hik_alarm_bind）

| 字段名 | 类型 | 说明 |
|--------|------|------|
| AlarmCameraIP | VARCHAR | 摄像头 IP（主键，用于匹配请求来源） |
| BlockComputerIP | VARCHAR | 目标电脑 IP（接收锁定/解锁） |
| HikAlarmCameraRoomName | VARCHAR | 关联房间/名称（可选） |

### 报警记录表（hik_alarm_record）

| 字段名 | 类型 | 说明 |
|--------|------|------|
| id | UUID | 主键 |
| event_type | VARCHAR | 事件类型 |
| event_time | TIMESTAMP | 事件时间 |
| device_name | VARCHAR | 设备名称 |
| channel_name | VARCHAR | 通道名称 |
| task_name | VARCHAR | 任务名称 |
| snapshot_base64_path | VARCHAR | 报警截图路径 |
| raw_data | TEXT | 原始 JSON 数据 |

## 日志配置

日志文件存储在 `logs` 目录下，按天滚动生成，保留最近 180 天的日志文件。

日志格式：
```
[2026-01-21 10:30:00 INF] WebCameraApi.Services.CameraRtspService | 获取摄像头【摄像头1】初始RTSP地址成功,现在开始拼接成完整RTSP
```

## Swagger 文档

项目启动后，访问以下地址查看 Swagger API 文档：

- 开发环境：`http://localhost:5283/swagger` 或 `https://localhost:7203/swagger`
- 生产环境：`http://0.0.0.0:12345/swagger`

## 项目结构

```
WebCameraApi/
├── Controllers/          # 控制器层
│   ├── CameraRtspController.cs    # 摄像头RTSP接口控制器
│   ├── HikACController.cs         # 海康门禁接口控制器
│   └── HikAlarmController.cs      # 海康报警接口控制器
├── Services/            # 服务层
│   ├── CameraRtspService.cs       # 摄像头RTSP服务
│   ├── HikAcService.cs            # 海康门禁服务
│   └── HikAlarmService.cs         # 海康报警服务
├── Dto/                 # 数据传输对象
│   ├── ApiResponseDto.cs          # 通用API响应格式
│   ├── CameraRtspRequest.cs       # 摄像头RTSP请求
│   ├── CameraRtspResponse.cs      # 摄像头RTSP响应
│   └── HikAcDto.cs                # 海康门禁 DTO（开门、人脸录制、人员下发/查询/删除、人脸下发等请求与响应）
├── Utils/               # 工具类
│   ├── ConfigGet/       # 配置获取工具
│   │   └── Appsettings_Get.cs
│   ├── HikAcessControl/ # 海康门禁工具
│   │   ├── Dto/HikAcDto.cs
│   │   └── Utils/CHCNetSDK.cs, HikAC.cs
│   ├── HikAlarmEndPoints/ # 海康报警工具
│   │   ├── Dto/HikAlarmRecordDto.cs
│   │   └── Util/
│   ├── OnvifManager/    # ONVIF管理工具
│   │   ├── DTO/         # ONVIF数据传输对象
│   │   └── Util/        # ONVIF工具类
│   └── PostgreConfig/   # PostgreSQL配置和仓储
│       ├── Dto/         # 数据库DTO
│       └── Util/        # 数据库仓储类
├── res/lib/             # 海康威视SDK文件
│   ├── HCNetSDK.dll     # 海康SDK核心库
│   ├── PlayCtrl.dll     # 播放控制库
│   └── HCNetSDKCom/     # SDK组件库
├── wwwroot/             # 静态文件目录
│   └── HikAlarmSnapshotBase64/ # 报警截图存储目录
├── logs/                # 日志文件目录
├── bin/                 # 编译输出目录
├── obj/                 # 编译中间文件目录
├── appsettings.json     # 开发环境配置
├── appsettings.Production.json # 生产环境配置
├── appsettings.Development.json # 开发环境配置
├── WebCameraApi.csproj  # 项目文件
├── Program.cs           # 程序入口
└── 打包.bat             # 打包脚本
```

## 注意事项

1. **海康威视 SDK**：项目依赖海康威视 SDK 文件，请确保 `res/lib` 目录下的所有 DLL 文件完整，包括 `HCNetSDK.dll`、`PlayCtrl.dll` 等
2. **数据库配置**：请根据实际环境修改 `appsettings.json` 中的数据库连接信息（host、database、username、password、port）
3. **端口配置**：
   - 开发环境：默认使用 5283（HTTP）和 7203（HTTPS）端口
   - 生产环境：默认使用 12345（HTTP）和 54321（HTTPS）端口
   - 可在 `appsettings.Production.json` 中修改 `Urls` 配置
4. **HTTPS 重定向**：项目默认启用 HTTPS 重定向，生产环境请配置 SSL 证书
5. **CORS 配置**：当前配置允许所有来源的跨域请求（`AllowAll` 策略），生产环境建议根据实际需求调整，仅允许可信来源
6. **日志管理**：
   - 日志文件存储在 `logs` 目录，按天滚动生成
   - 默认保留最近 180 天的日志文件
   - 生产环境建议定期备份重要日志
7. **图片存储**：报警截图自动保存到 `wwwroot/HikAlarmSnapshotBase64` 目录，请确保磁盘空间充足
8. **防抖机制**：人数计数接口默认使用 1000ms 防抖时间，避免短时间内重复发送相同请求
9. **超时设置**：HTTP 请求默认超时时间为 3 秒，可在 `Program.cs` 中调整
10. **并发处理**：报警服务使用并发安全字典处理高并发场景，确保数据一致性
11. **事件类型翻译**：报警记录查询会自动将英文事件类型翻译为中文（如 `uniformDetection` → `制服检测`）
12. **容器化支持**：项目支持 Docker 容器化部署，配置了 `ContainerBaseImage` 和 `ContainerPort`
13. **来源 IP 依赖**：人数计数接口依赖请求来源 IP 进行绑定匹配，若经反向代理或 NAT 转发，需确保 Remote IP 与 `AlarmCameraIP` 一致
14. **图片可访问性**：行为分析接口会下载 `resourcesContent` 的图片 URL，要求 API 服务器能直接访问该 URL（若需要鉴权需改造代码）
15. **绑定配置加载**：`hik_alarm_bind` 绑定信息在服务启动时加载，数据库修改后需重启服务生效
16. **人数阈值固定**：人数计数阈值当前固定为 `< 3` 触发锁定，如需可配置化需调整代码
17. **门禁端口说明**：门禁设备 8000 端口为海康 SDK 私有协议（登录、开门、查询/下发人员、查删除进度等）；HTTP ISAPI（如删除用户命令 PUT）走 80 端口。删除用户接口会先通过 HTTP 80 下发删除命令，再通过 SDK 8000 轮询删除进度

## 许可证

本项目仅供内部使用，请勿用于商业用途。

## 常见问题

### 1. 如何添加新的摄像头配置？

在 PostgreSQL 数据库的 `onvif_camera_infomation` 表中插入新记录：

```sql
INSERT INTO onvif_camera_infomation (id, camera_name, ip, port, user, password, retry_count, wait_milliseconds)
VALUES (gen_random_uuid(), '新摄像头', '192.168.1.100', 80, 'admin', 'password', 3, 1000);
```

### 2. 如何添加新的门禁配置？

无需入库，前端调用开门接口时直接传入门禁信息即可（见接口文档示例）。

### 3. 如何配置报警绑定？

在 PostgreSQL 数据库的 `hik_alarm_bind` 表中插入绑定配置：

```sql
INSERT INTO hik_alarm_bind (AlarmCameraIP, BlockComputerIP, HikAlarmCameraRoomName)
VALUES ('192.168.1.100', '192.168.1.50', '审讯室1');
```

### 4. 如何查看日志？

日志文件存储在 `logs` 目录，文件名格式为 `Log-YYYYMMDD.txt`。可以使用文本编辑器或日志查看工具打开。

### 5. 如何修改防抖时间？

在 `HikAlarmService.cs` 中修改 `_debounceMilliseconds` 常量的值（默认为 1000 毫秒）。

### 6. 如何修改 HTTP 请求超时时间？

在 `Program.cs` 中修改 HttpClient 的超时配置（默认为 3000 毫秒）。

### 7. 报警截图存储在哪里？

报警截图存储在 `wwwroot/HikAlarmSnapshotBase64` 目录，文件名格式为 `{设备名}_{事件类型}_{时间戳}.jpg`。

### 8. 如何部署到生产环境？

1. 修改 `appsettings.Production.json` 中的数据库连接和 URL 配置
2. 运行 `打包.bat` 脚本进行打包
3. 将打包后的文件部署到生产服务器
4. 配置 SSL 证书（如需要）
5. 启动服务：`dotnet WebCameraApi.dll --environment Production`

### 9. 如何解决摄像头 RTSP 获取失败问题？

- 检查摄像头是否支持 ONVIF 协议
- 确认摄像头的 IP、端口、用户名、密码配置正确
- 检查网络连通性
- 查看 `logs` 目录下的日志文件，获取详细错误信息
- 尝试增加 `retry_count` 和 `wait_milliseconds` 参数

### 10. 如何解决门禁开门失败问题？

- 检查门禁设备是否支持海康威视 SDK
- 确认门禁的 IP、端口、用户名、密码配置正确
- 检查网络连通性
- 查看 `logs` 目录下的日志文件，获取详细错误信息
- 确认门禁设备在线且未被锁定

### 10.1 如何删除门禁用户？删除失败怎么办？

- 调用 `POST /api/HikAC/deleteUser`，请求体包含 `devices`（门禁设备列表）和 `userID`（要删除的人员工号）
- 删除会同时删除该用户的卡、指纹、人脸信息
- 删除命令通过 HTTP ISAPI（端口 80）下发，删除进度通过 SDK（端口 8000）轮询；需确保设备 80 端口可访问（ISAPI），8000 端口可访问（SDK 登录与进度查询）
- 若提示“下发删除命令失败”：检查设备 80 端口是否开放、账号密码是否正确、设备是否支持 ISAPI 删除
- 若提示“查询删除进度超时”或“建立长连接失败”：检查 8000 端口与 SDK 登录是否正常，查看日志中的 `UserInfoDetailDeleteProcess` 返回内容

### 11. 如何启用 HTTPS？

1. 获取 SSL 证书（.pfx 或 .pem 格式）
2. 在 `appsettings.Production.json` 中配置证书路径和密码
3. 或使用 IIS、Nginx 等反向代理配置 HTTPS

### 12. 如何修改日志保留天数？

在 `Program.cs` 中修改 `retainedFileCountLimit` 参数（默认为 180 天）。

## 开发指南

### 本地开发

1. 克隆项目到本地
2. 配置 `appsettings.json` 中的数据库连接
3. 运行 `dotnet restore` 恢复依赖
4. 运行 `dotnet run` 启动开发服务器
5. 访问 `http://localhost:5283/swagger` 查看 API 文档

### 调试

1. 使用 Visual Studio 或 VS Code 打开项目
2. 设置断点
3. 按 F5 启动调试
4. 使用 Swagger 或 Postman 测试接口

### 代码规范

- 遵循 C# 命名规范
- 使用 XML 注释文档化公共 API
- 保持代码简洁清晰，避免过度设计
- 使用 Serilog 记录关键操作和异常信息

## 版本历史

- **v1.0.0**：初始版本，支持摄像头 RTSP 获取、门禁控制、报警处理功能
- **v1.1.0**：新增报警记录统计接口，优化防抖机制，增强并发处理能力
- **v1.2.0**：门禁能力扩展：人脸录制（recordFace）、下发人员（addUser）、查询人员（queryUsers）、下发人脸（addFace）、删除门禁用户（deleteUser）；删除用户采用 HTTP ISAPI 下发命令 + SDK 轮询进度，兼容设备 80/8000 端口
