using System;
using Azure.Identity;
using Azure.Core;
using Azure.Core.Diagnostics;

class Program
{
    static void Main(string[] args)
    {
        // Enable detailed Azure Identity logs in the console
        using AzureEventSourceListener listener = AzureEventSourceListener.CreateConsoleLogger();

        try
        {
            // DefaultAzureCredential vai tentar várias opções (CLI, VS, Managed Identity, etc.)
            var credential = new DefaultAzureCredential();

            // Força um token contra o Azure Resource Manager
            var token = credential.GetToken(
                new TokenRequestContext(
                    new[] { "https://management.azure.com/.default" }
                ),
                default
            );

            Console.WriteLine("✅ Token obtido com sucesso!");
            Console.WriteLine($"Expira em: {token.ExpiresOn}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Falha ao obter token:");
            Console.WriteLine(ex.ToString());
        }
    }
}
