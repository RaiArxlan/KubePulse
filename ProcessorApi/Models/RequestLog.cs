using System;
namespace ProcessorApi.Models
{
    public class RequestLog
    {
        public Guid Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string SourceService { get; set; } = string.Empty;
    }
}
