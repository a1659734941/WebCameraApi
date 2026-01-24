using System.Collections.Generic;

namespace WebCameraApi.Dto
{
    public class BatchImportResultDto
    {
        public int Total { get; set; }
        public int Success { get; set; }
        public int Failed { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class CameraConfigDto
    {
        public string CameraName { get; set; }
        public string CameraIP { get; set; }
        public string CameraUser { get; set; }
        public string CameraPassword { get; set; }
        public int CameraPort { get; set; }
        public int CameraRetryCount { get; set; }
        public int CameraWaitmillisecounds { get; set; }
    }

    public class HikAcBatchConfigDto
    {
        public string HikAcName { get; set; }
        public string HikAcIP { get; set; }
        public string HikAcUser { get; set; }
        public string HikAcPassword { get; set; }
        public int HikAcPort { get; set; }
        public int HikAcRetryCount { get; set; }
        public int HikAcWaitmillisecounds { get; set; }
    }
}
