namespace DocCollector_SignalR;

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
internal sealed partial class Api
{
    public record LoanDocument(string Name)
    {
        public List<DocFields> Fields { get; } = [];

        public record DocFields(string FieldName, string Description);
    }
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}
