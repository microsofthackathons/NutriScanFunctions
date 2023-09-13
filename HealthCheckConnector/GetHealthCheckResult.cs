using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Storage.Blobs;
using HealthCheckConnector.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HealthCheckConnector
{
    public static class GetHealthCheckResult
    {
        [FunctionName("GetHealthCheckResult")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                Contracts.AnalyzeResult analyzeResult = null;
                log.LogInformation("C# HTTP trigger function processed a request.");

                string name = req.Query["name"];

                var storageConnString = Environment.GetEnvironmentVariable("storageConnectionString");
                BlobClient blobClient = new BlobClient(storageConnString, Environment.GetEnvironmentVariable("blobContainerName"), name);

                using (var stream = await blobClient.OpenReadAsync())
                {
                    analyzeResult = await AnalyzeDocument(stream);
                }
                if (analyzeResult != null)
                    return new OkObjectResult(analyzeResult);
            }
            catch (Exception ex)
            {
                log.LogError($"GetHealthCheckResult Function failed. Error details : {ex.Message + ex.InnerException}");
            }
            string responseMessage = $"Something went wrong.";
            return new ObjectResult(responseMessage);
        }

        private static async Task<Contracts.AnalyzeResult> AnalyzeDocument(Stream fileStream)
        {
            Contracts.AnalyzeResult analyzeResult = null;
            try
            {
                string endpoint = Environment.GetEnvironmentVariable("cognitiveApiEndpoint");
                string apiKey = Environment.GetEnvironmentVariable("cognitiveApiKey");
                AzureKeyCredential credential = new AzureKeyCredential(apiKey);
                DocumentAnalysisClient client = new DocumentAnalysisClient(new Uri(endpoint), credential);

                string modelId = Environment.GetEnvironmentVariable("cognitiveApiModelId");

                AnalyzeDocumentOperation operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, modelId, fileStream);
                Azure.AI.FormRecognizer.DocumentAnalysis.AnalyzeResult result = operation.Value;

                if (result != null)
                {
                    analyzeResult = new Contracts.AnalyzeResult
                    {
                        Age = (!string.IsNullOrWhiteSpace((result.Documents.FirstOrDefault().Fields.Where(f => f.Key.Equals("Age") || f.Key.Equals("age"))).FirstOrDefault().Value?.Content)) ? Convert.ToString((result.Documents.FirstOrDefault().Fields.Where(f => f.Key.Equals("Age") || f.Key.Equals("age"))).FirstOrDefault().Value?.Content) : string.Empty,
                        Name = (!string.IsNullOrWhiteSpace((result.Documents.FirstOrDefault().Fields.Where(f => f.Key.Equals("Name") || f.Key.Equals("name"))).FirstOrDefault().Value?.Content)) ? Convert.ToString((result.Documents.FirstOrDefault().Fields.Where(f => f.Key.Equals("Name") || f.Key.Equals("name"))).FirstOrDefault().Value?.Content) : string.Empty,
                        Gender = (!string.IsNullOrWhiteSpace((result.Documents.FirstOrDefault().Fields.Where(f => f.Key.Equals("Gender") || f.Key.Equals("gender"))).FirstOrDefault().Value?.Content)) ? Convert.ToString((result.Documents.FirstOrDefault().Fields.Where(f => f.Key.Equals("Gender") || f.Key.Equals("gender"))).FirstOrDefault().Value?.Content) : string.Empty
                    };

                    /*
                    var reportTable = result.Tables.LastOrDefault();
                    int columnCount = reportTable.ColumnCount;

                    int loopIndex = reportTable.Cells.Where(c => c.Kind.Equals("content")).Count();
                    analyzeResult.ReportFields = new List<ReportField>();
                    ReportField reportField = new();
                    for (int colCount = loopIndex; colCount < reportTable.Cells.Count; colCount++)
                    {
                        reportField = new()
                        {
                            Name = reportTable.Cells[colCount++]?.Content?.ToString(),
                            Value = reportTable.Cells[colCount++]?.Content?.ToString(),
                            Unit = reportTable.Cells[colCount++]?.Content?.ToString(),
                            Range = reportTable.Cells[colCount]?.Content?.ToString()
                        };
                       
                        analyzeResult.ReportFields.Add(reportField);
                        if (columnCount > 4)
                            colCount = colCount + (columnCount - 4);
                    }
                    */

                    var reportTable = result.Tables.LastOrDefault();
                    analyzeResult.ReportFields = new List<ReportField>();

                    ReportField reportField = new();
                    int hbCellIndex = reportTable.Cells.ToList().IndexOf(reportTable.Cells.Where(c => c.Content.ToLower().Contains("haemoglobin") || c.Content.ToLower().Contains("hemoglobin")).FirstOrDefault());
                    if (hbCellIndex >= 0)
                    {
                        reportField.Name = reportTable.Cells[hbCellIndex].Content;
                        reportField.Value = reportTable.Cells[hbCellIndex + 1].Content;
                        reportField.Unit = reportTable.Cells[hbCellIndex + 2].Content;
                        reportField.Range = reportTable.Cells[hbCellIndex + 3].Content;
                        analyzeResult.ReportFields.Add(reportField);
                    }

                    hbCellIndex = reportTable.Cells.ToList().IndexOf(reportTable.Cells.Where(c => c.Content.ToLower().Contains("rbc count")).FirstOrDefault());
                    if (hbCellIndex >= 0)
                    {
                        reportField = new();
                        reportField.Name = reportTable.Cells[hbCellIndex].Content;
                        reportField.Value = reportTable.Cells[hbCellIndex + 1].Content;
                        reportField.Unit = reportTable.Cells[hbCellIndex + 2].Content;
                        reportField.Range = reportTable.Cells[hbCellIndex + 3].Content;
                        analyzeResult.ReportFields.Add(reportField);
                    }
                }

                Console.WriteLine($"Document was analyzed with model with ID: {result.ModelId}");
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return analyzeResult;
        }
    }
}
