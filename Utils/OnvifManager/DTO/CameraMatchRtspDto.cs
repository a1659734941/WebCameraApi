using System;
using System.Collections.Generic;

namespace Onvif_GetPhoto.DTO
{
    /// <summary>
    /// 单个摄像头与RTSP地址的映射DTO（数据传输对象）
    /// </summary>
    public class CameraMatchRtspDto
    {
        /// <summary>
        /// 摄像头名称（唯一标识）
        /// </summary>
        public string CameraName { get; set; } = string.Empty;

        /// <summary>
        /// 摄像头对应的RTSP地址
        /// </summary>
        public string RtspUrl { get; set; } = string.Empty;

        /// <summary>
        /// 摄像头用户名
        /// </summary>
        public string CameraUserName { get; set; } = string.Empty;

        /// <summary>
        /// 摄像头密码
        /// </summary>
        public string CameraPassword { get; set; } = string.Empty;

        /// <summary>
        /// 摄像头IP地址
        /// </summary>
        public string CameraIP { get; set; } = string.Empty; // 新增输入字段
    }

    /// <summary>
    /// 摄像头RTSP信息封装类（存储字典的核心值）
    /// </summary>
    public class CameraRtspInfo
    {
        /// <summary>
        /// RTSP地址
        /// </summary>
        public string RtspUrl { get; set; } = string.Empty;

        /// <summary>
        /// 摄像头用户名
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 摄像头密码
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 摄像头IP地址
        /// </summary>
        public string CameraIP { get; set; } = string.Empty; // 新增存储字段
    }

    /// <summary>
    /// 摄像头RTSP地址字典管理器（静态类，全局统一管理）
    /// </summary>
    public static class CameraRtspManager
    {
        // 核心字典：键=摄像头名称，值=完整的RTSP信息（包含地址、用户名、密码、IP）
        private static readonly Dictionary<string, CameraRtspInfo> _cameraRtspDict = new Dictionary<string, CameraRtspInfo>();

        /// <summary>
        /// 线程安全锁
        /// </summary>
        private static readonly object _lockObj = new object();

        /// <summary>
        /// 添加/更新摄像头的RTSP信息（包含地址、用户名、密码、IP）
        /// </summary>
        /// <param name="cameraRtspDto">单个摄像头的RTSP DTO</param>
        /// <exception cref="ArgumentNullException">DTO或关键字段为空时抛出</exception>
        public static void AddOrUpdateCameraRtsp(CameraMatchRtspDto cameraRtspDto)
        {
            // 参数校验
            if (cameraRtspDto == null)
                throw new ArgumentNullException(nameof(cameraRtspDto), "摄像头RTSP DTO不能为空");
            if (string.IsNullOrWhiteSpace(cameraRtspDto.CameraName))
                throw new ArgumentNullException(nameof(cameraRtspDto.CameraName), "摄像头名称不能为空");

            // 封装完整的RTSP信息（包含新字段）
            var rtspInfo = new CameraRtspInfo
            {
                RtspUrl = cameraRtspDto.RtspUrl,
                UserName = cameraRtspDto.CameraUserName,
                Password = cameraRtspDto.CameraPassword,
                CameraIP = cameraRtspDto.CameraIP // 同步新字段
            };

            // 线程安全写入字典
            lock (_lockObj)
            {
                if (_cameraRtspDict.ContainsKey(cameraRtspDto.CameraName))
                {
                    _cameraRtspDict[cameraRtspDto.CameraName] = rtspInfo; // 更新完整信息
                }
                else
                {
                    _cameraRtspDict.Add(cameraRtspDto.CameraName, rtspInfo); // 添加完整信息
                }
            }
        }

        /// <summary>
        /// 根据摄像头名称获取完整的RTSP信息（地址+用户名+密码+IP）
        /// </summary>
        /// <param name="cameraName">摄像头名称</param>
        /// <param name="rtspInfo">输出：匹配到的完整RTSP信息（未找到则为null）</param>
        /// <returns>是否找到对应的信息</returns>
        public static bool TryGetCameraRtspInfo(string cameraName, out CameraRtspInfo? rtspInfo)
        {
            lock (_lockObj)
            {
                return _cameraRtspDict.TryGetValue(cameraName, out rtspInfo);
            }
        }

        /// <summary>
        /// 【兼容旧代码】根据摄像头名称仅获取RTSP地址
        /// </summary>
        /// <param name="cameraName">摄像头名称</param>
        /// <param name="rtspUrl">输出：匹配到的RTSP地址（未找到则为空）</param>
        /// <returns>是否找到对应的RTSP地址</returns>
        public static bool TryGetRtspUrl(string cameraName, out string rtspUrl)
        {
            rtspUrl = string.Empty;
            if (TryGetCameraRtspInfo(cameraName, out var rtspInfo))
            {
                rtspUrl = rtspInfo?.RtspUrl ?? string.Empty;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 清空所有摄像头RTSP映射
        /// </summary>
        public static void ClearAll()
        {
            lock (_lockObj)
            {
                _cameraRtspDict.Clear();
            }
        }

        /// <summary>
        /// 获取所有摄像头RTSP映射（只读）
        /// </summary>
        public static IReadOnlyDictionary<string, CameraRtspInfo> GetAllCameraRtsp()
        {
            lock (_lockObj)
            {
                return new Dictionary<string, CameraRtspInfo>(_cameraRtspDict);
            }
        }
    }
}