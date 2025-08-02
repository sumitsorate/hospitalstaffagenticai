namespace HospitalSchedulingApp.Dtos.Agent
{
    public class AgentSummaryResponseDto
    {
        public string SummaryMessage { get; set; } = string.Empty;
        public List<QuickReply> QuickReplies { get; set; } = new();
    }

    public class QuickReply
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
