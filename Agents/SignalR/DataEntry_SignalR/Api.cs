namespace DataEntry_SignalR;

using System.ComponentModel;
using System.Threading.Tasks;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
internal class Api(Kernel kernel, PromptExecutionSettings promptExecutionSettings)
{
    public record class PromptResponse(string FieldName, string Prompt);

    [Description("Prompts the user for responses to fill out a set of fields, given the field names and descriptions.")]
    [return: Description("A dictionary of field names and the corresponding user responses.")]
    public async Task<PromptResponse> PromptToFillAsync(
        [Description("A dictionary of field names and descriptions. If a field has already been filled by the user, the description should be replaced with \"`RESPONSE` = <user's response>\"")]
        Dictionary<string, string> data)
    {
        var settings = OpenAIPromptExecutionSettings.FromExecutionSettings(promptExecutionSettings);
        var response = await kernel.InvokePromptAsync($@"Here are the set of fields the user needs to fill out. If a description begins with `RESPONSE`, then it does *not* need to be filled out, but rather the description IS the user's response that they've already furnished after being prompted for it.
So, you need only prompt for field which does not have a description beginning with `RESPONSE` by asking the user an appropriate question to get the answer to fill the field out.

FIELDS
{string.Join('\n', data.Select(i => $"{i.Key} - {i.Value}"))}

To properly prompt the user, you must return a JSON object of the following format:
{{
    ""FieldName"": ""<name of the field>"",
    ""Prompt"" : ""<prompt to ask the user to fill out the field>""
}}

Remember, you must only ask for one field at a time.", new(settings)).ConfigureAwait(false);

        return response.GetValue<PromptResponse>()!;
    }
}
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
