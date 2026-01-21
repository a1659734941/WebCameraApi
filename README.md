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
- 通过门禁名称或 IP 地址控制门禁开门
- 从 PostgreSQL 数据库读取门禁配置
- 使用海康威视 SDK 控制门禁
- 支持多种门禁设备管理

### 3. 海康报警/计数服务
- **人数计数接口**：接收摄像头人数计数数据，当人数少于阈值时自动发送锁定/解锁请求
- **行为分析接口**：接收摄像头行为分析 JSON 数据，包括制服检测、人数统计、超时逗留、起立检测、攀高检测等
- **报警记录查询**：支持按时间范围、事件类型、设备名称筛选，支持分页查询
- **图片下载和存储**：自动下载报警截图并保存到本地，返回相对路径供前端访问
- **防抖机制**：避免同一 IP 短时间内重复发送相同请求

## 技术栈

- **框架**：.NET 8.0
- **数据库**：PostgreSQL
- **日志**：Serilog（支持控制台和文件输出）
- **API 文档**：Swagger
- **协议支持**：ONVIF（摄像头）、海康威视 SDK（门禁和报警）
- **其他**：Newtonsoft.Json、Npgsql、System.Text.Encoding.CodePages

## 环境要求

- **操作系统**：Windows x64
- **.NET SDK**：.NET 8.0 或更高版本
- **数据库**：PostgreSQL 10.0 或更高版本
- **依赖库**：海康威视 SDK 文件（已包含在 `res/lib` 目录）

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

## API 接口文档

### 1. 摄像头 RTSP 接口

#### 获取 RTSP 地址（GET）

**接口地址**：`/api/CameraRtsp/GetRtsp`

**请求参数**：
- `cameraName`（必填）：摄像头名称

**请求示例**：
```
GET /api/CameraRtsp/GetRtsp?cameraName=摄像头1
```

**响应示例**：
```json
{
  "code": 200,
  "message": "获取RTSP地址成功",
  "data": {
    "cameraName": "摄像头1",
    "rtspUrl": "rtsp://username:password@192.168.1.100:554/Streaming/Channels/101"
  }
}
```

#### 获取 RTSP 地址（POST）

**接口地址**：`/api/CameraRtsp/GetRtsp`

**请求参数**：
- `cameraName`（必填）：摄像头名称

**请求示例**：
```json
POST /api/CameraRtsp/GetRtsp
Content-Type: application/json

"摄像头1"
```

### 2. 门禁控制接口

#### 开启门禁

**接口地址**：`/api/HikAC/openHikAC`

**请求参数**：
- `acName`（可选）：门禁名称
- `acIp`（可选）：门禁 IP 地址

**注意**：`acName` 和 `acIp` 至少传入一个

**请求示例**：
```
GET /api/HikAC/openHikAC?acName=门禁1
```

或

```
GET /api/HikAC/openHikAC?acIp=192.168.1.200
```

**响应示例**：
```json
{
  "code": 200,
  "message": "门禁 : 门禁1开门成功!",
  "data": {
    "acName": "门禁1"
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

### 门禁配置表（hik_ac_infomation）

| 字段名 | 类型 | 说明 |
|--------|------|------|
| id | UUID | 主键 |
| hik_ac_name | VARCHAR | 门禁名称 |
| hik_ac_ip | VARCHAR | IP 地址 |
| hik_ac_port | INTEGER | 端口号 |
| hik_ac_user | VARCHAR | 用户名 |
| hik_ac_password | VARCHAR | 密码 |

### 报警绑定配置表（hik_alarm_bind）

| 字段名 | 类型 | 说明 |
|--------|------|------|
| key | VARCHAR | 键（IP 地址） |
| value | JSON | 值（包含 BlockComputerIP 等） |

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

- 开发环境：`http://localhost:5000/swagger`
- 生产环境：`http://0.0.0.0:12345/swagger`

## 项目结构

```
WebCameraApi/
├── Controllers/          # 控制器层
│   ├── CameraRtspController.cs
│   ├── HikACController.cs
│   └── HikAlarmController.cs
├── Services/            # 服务层
│   ├── CameraRtspService.cs
│   ├── HikAcService.cs
│   └── HikAlarmService.cs
├── Dto/                 # 数据传输对象
├── Utils/               # 工具类
│   ├── ConfigGet/       # 配置获取
│   ├── HikAcessControl/ # 海康门禁
│   ├── HikAlarmEndPoints/ # 海康报警
│   ├── OnvifManager/    # ONVIF 管理
│   └── PostgreConfig/   # PostgreSQL 配置
├── res/lib/             # 海康威视 SDK 文件
├── wwwroot/             # 静态文件目录
│   └── HikAlarmSnapshotBase64/ # 报警截图存储目录
├── logs/                # 日志文件目录
├── appsettings.json     # 开发环境配置
├── appsettings.Production.json # 生产环境配置
└── Program.cs           # 程序入口
```

## 注意事项

1. **海康威视 SDK**：项目依赖海康威视 SDK 文件，请确保 `res/lib` 目录下的所有 DLL 文件完整
2. **数据库配置**：请根据实际环境修改 `appsettings.json` 中的数据库连接信息
3. **端口配置**：生产环境默认使用 12345（HTTP）和 54321（HTTPS）端口，可根据需要修改
4. **HTTPS 重定向**：项目默认启用 HTTPS 重定向，生产环境请配置 SSL 证书
5. **CORS 配置**：当前配置允许所有来源的跨域请求，生产环境建议根据实际需求调整

## 许可证

本项目仅供内部使用，请勿用于商业用途。