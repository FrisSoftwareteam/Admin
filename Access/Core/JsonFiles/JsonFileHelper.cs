using FirstReg.OnlineAccess.JsonFiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FirstReg.OnlineAccess;

internal static class FileKeys
{
    internal static class FidelityRights2024
    {
        public const string ListNotSent = "fidelity-rights-2024-list-not-sent.json";
        public const string ListFailed = "fidelity-rights-2024-list-of-failed.json";
    }
    internal static class OandoShareholder2025
    {
        public const string ListNotSent = "oando-shareholder-list-2025.json";
    }
}

public static class JsonFileHelper
{
    public static async Task<List<ShareholderInformationUpdate.ListNotSent>> ReadFidelityRights2024ListNotSentAsync()
    {
        return await ReadJsonFileAsync<ShareholderInformationUpdate.ListNotSent>(FileKeys.FidelityRights2024.ListNotSent);
    }

    public static async Task<List<ShareholderInformationUpdate.ListFailed>> ReadFidelityRights2024ListOfFailedAsync()
    {
        return await ReadJsonFileAsync<ShareholderInformationUpdate.ListFailed>(FileKeys.FidelityRights2024.ListFailed);
    }

    public static async Task<List<ShareholderInformationUpdate.ListNotSent>> ReadOandoShareholderAsync()
    {
        return await ReadJsonFileAsync<ShareholderInformationUpdate.ListNotSent>(FileKeys.OandoShareholder2025.ListNotSent);
    }

    private static async Task<List<T>> ReadJsonFileAsync<T>(string fileName)
    {
        string filePath = Path.Combine("wwwroot", "data", fileName);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The file '{filePath}' was not found.");
        }

        try
        {
            using FileStream openStream = File.OpenRead(filePath);
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            return await JsonSerializer.DeserializeAsync<List<T>>(openStream, options)
                   ?? new List<T>();
        }
        catch (JsonException ex)
        {
            // Log the exception (logging mechanism not shown here)
            throw new InvalidOperationException("Failed to deserialize JSON file.", ex);
        }
    }
}