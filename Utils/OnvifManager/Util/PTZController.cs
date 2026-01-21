using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Onvif_GetPhoto.DTO;
using XiaoFeng.Onvif;

namespace Onvif_GetPhoto.Util
{
    public class PTZController
    {
        /// <summary>
        /// 执行PTZ操作
        /// </summary>
        /// <param name="operation">PTZ操作类型</param>
        /// <param name="Ip">设备IP</param>
        /// <param name="user">设备登录用户名</param>
        /// <param name="pass">设备登录密码</param>
        /// <param name="port">设备端口</param>
        /// <param name="x">Pan/Tilt/X轴参数</param>
        /// <param name="y">Tilt/Y轴参数</param>
        /// <param name="z">Zoom/Z轴参数</param>
        /// <returns>操作结果（服务返回的字符串列表）</returns>
        public static async Task<List<string>> PTZOperationController(
            PTZOperation operation,
            string Ip,
            string user,
            string pass,
            int port,
            double x = 0, double y = 0, double z = 0)
        {
            try
            {
                // 初始化设备端点
                var iPEndPoint = new IPEndPoint(IPAddress.Parse(Ip), port);
                Console.WriteLine($"[初始化] 设备端点创建成功：{Ip}:{port}");

                // 获取设备UTC时间
                Console.WriteLine($"[时间同步] 开始获取设备UTC时间...");
                var onvifUTCDateTime = await DeviceUtcTime.getDeviceUtcTimeAsync(Ip, port, "中国");
                Console.WriteLine($"[时间同步] 设备UTC时间获取成功：{onvifUTCDateTime:yyyy-MM-dd HH:mm:ss}");

                // 获取媒体配置文件Token
                Console.WriteLine($"[配置获取] 开始获取媒体配置文件Token...");
                var tokens = await MediaService.GetProfiles(iPEndPoint, user, pass, onvifUTCDateTime);
                if (tokens == null || tokens.Count == 0)
                {
                    Console.WriteLine($"[配置获取] 失败：未获取到有效媒体配置文件Token");
                    return null;
                }
                var profileToken = tokens[0];
                Console.WriteLine($"[配置获取] 成功，使用第一个Token：{profileToken}");

                // 提取公共参数，减少重复代码
                var ptzParams = (iPEndPoint, user, pass, onvifUTCDateTime, profileToken);
                Console.WriteLine($"[操作准备] 开始执行PTZ操作：{operation}");

                // 执行对应PTZ操作（带详细日志）
                switch (operation)
                {
                    case PTZOperation.GetStatus:
                        Console.WriteLine($"[状态查询] 开始获取PTZ当前状态...");
                        var statusResult = await PTZService.GetStatus(
                            ptzParams.iPEndPoint, ptzParams.user, ptzParams.pass, 
                            ptzParams.onvifUTCDateTime, ptzParams.profileToken);
                        Console.WriteLine($"[状态查询] 操作完成，返回结果条数：{statusResult?.Count ?? 0}");
                        return statusResult;

                    case PTZOperation.AbsoluteMove:
                        Console.WriteLine($"[绝对移动] 参数：X={x}, Y={y}，开始执行...");
                        var absMoveResult = await PTZService.AbsoluteMove(
                            ptzParams.iPEndPoint, ptzParams.user, ptzParams.pass, 
                            ptzParams.onvifUTCDateTime, ptzParams.profileToken, x, y);
                        Console.WriteLine($"[绝对移动] 操作完成，返回结果：{string.Join(";", absMoveResult ?? new List<string>())}");
                        return absMoveResult;

                    case PTZOperation.Stop:
                        Console.WriteLine($"[停止操作] 开始执行PTZ停止命令...");
                        var stopResult = await PTZService.Stop(
                            ptzParams.iPEndPoint, ptzParams.user, ptzParams.pass, 
                            ptzParams.onvifUTCDateTime, ptzParams.profileToken);
                        Console.WriteLine($"[停止操作] 执行结果：{string.Join(";", stopResult ?? new List<string>())}");
                        return stopResult;

                    case PTZOperation.ContinuousMove:
                        Console.WriteLine($"[连续移动] 参数：X速度={x}, Y速度={y}, Z速度={z}，开始执行...");
                        var contMoveResult = await PTZService.ContinuousMove(
                            ptzParams.iPEndPoint, ptzParams.user, ptzParams.pass, 
                            ptzParams.onvifUTCDateTime, ptzParams.profileToken, x, y, z);
                        Console.WriteLine($"[连续移动] 操作完成，返回结果：{string.Join(";", contMoveResult ?? new List<string>())}");
                        return contMoveResult;

                    case PTZOperation.SetHomePosition:
                        Console.WriteLine($"[设置原点] 开始设置PTZ默认原点位置...");
                        var setHomeResult = await PTZService.SetHomePosition(
                            ptzParams.iPEndPoint, ptzParams.user, ptzParams.pass, 
                            ptzParams.onvifUTCDateTime, ptzParams.profileToken);
                        Console.WriteLine($"[设置原点] 执行结果：{string.Join(";", setHomeResult ?? new List<string>())}");
                        return setHomeResult;

                    case PTZOperation.GotoHomePosition:
                        Console.WriteLine($"[回归原点] 参数：X速度={x}, Y速度={y}, Z速度={z}，开始执行...");
                        var gotoHomeResult = await PTZService.GotoHomePosition(
                            ptzParams.iPEndPoint, ptzParams.user, ptzParams.pass, 
                            ptzParams.onvifUTCDateTime, ptzParams.profileToken, x, y, z);
                        Console.WriteLine($"[回归原点] 操作完成，返回结果：{string.Join(";", gotoHomeResult ?? new List<string>())}");
                        return gotoHomeResult;

                    case PTZOperation.RelativeMove:
                        Console.WriteLine($"[相对移动] 参数：X偏移={x}, Y偏移={y}, Z偏移={z}，开始执行...");
                        var relMoveResult = await PTZService.RelativeMove(
                            ptzParams.iPEndPoint, ptzParams.user, ptzParams.pass, 
                            ptzParams.onvifUTCDateTime, ptzParams.profileToken, x, y, z);
                        Console.WriteLine($"[相对移动] 操作完成，返回结果：{string.Join(";", relMoveResult ?? new List<string>())}");
                        return relMoveResult;

                    default:
                        var errorMsg = $"不支持的PTZ操作类型：{operation}";
                        Console.WriteLine($"[参数错误] {errorMsg}");
                        throw new ArgumentOutOfRangeException(nameof(operation), errorMsg);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[操作异常] 设备初始化或PTZ操作失败：{ex.Message}");
                Console.WriteLine($"[异常堆栈] {ex.StackTrace}");
                return null;
            }
        }
    }
}