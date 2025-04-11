namespace TBAAPI.V3Client.Api;

#pragma warning disable CS8019
using Microsoft.Extensions.Logging;
using System;
#pragma warning restore CS8019

static partial class Log
{

    [LoggerMessage(0, LogLevel.Debug, "Resulting document: {searchResults}")]
    internal static partial void ResultingDocumentSearchResults(this ILogger logger, string searchResults);

    [LoggerMessage(2, LogLevel.Trace, "JsonCons.JMESPath result: {jsonConsResult}")]
    internal static partial void JsonConsJMESPathResultJsonConsResult(this ILogger logger, string jsonConsResult);
}
