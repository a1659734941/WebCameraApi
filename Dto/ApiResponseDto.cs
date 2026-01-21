namespace WebCameraApi.Dto
{
    /// <summary>
    /// 通用接口返回格式
    /// </summary>
    /// <typeparam name="T">数据泛型</typeparam>
    public class ApiResponseDto<T>
    {
        /// <summary>
        /// 状态码
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// 提示信息
        /// </summary>
        public string Msg { get; set; } = string.Empty;

        /// <summary>
        /// 业务数据
        /// </summary>
        public T Data { get; set; } = default!;

        /// <summary>
        /// 成功响应
        /// </summary>
        /// <param name="data">业务数据</param>
        /// <param name="msg">提示信息</param>
        /// <returns></returns>
        public static ApiResponseDto<T> Success(T data, string msg = "请求成功")
        {
            return new ApiResponseDto<T>
            {
                Code = 200,
                Msg = msg,
                Data = data
            };
        }

        /// <summary>
        /// 失败响应
        /// </summary>
        /// <param name="msg">提示信息</param>
        /// <param name="code">状态码</param>
        /// <returns></returns>
        public static ApiResponseDto<T> Fail(string msg, int code = 400)
        {
            return new ApiResponseDto<T>
            {
                Code = code,
                Msg = msg,
                Data = default!
            };
        }
    }

    // 无数据返回的重载
    public class ApiResponseDto : ApiResponseDto<object>
    {
        public static new ApiResponseDto Success(string msg = "请求成功")
        {
            return new ApiResponseDto
            {
                Code = 200,
                Msg = msg,
                Data = null
            };
        }
    }
}