using System.Collections.Generic;

namespace HealthCheckConnector.Contracts
{
    internal class AnalyzeResult
    {
        public string Age { get; set; }
        public string Gender { get; set; }
        public string Name { get; set; }
        public List<ReportField> ReportFields { get; set; }
    }

    internal class ReportField
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Unit { get; set; }
        public string Range { get; set; }
    }
}
