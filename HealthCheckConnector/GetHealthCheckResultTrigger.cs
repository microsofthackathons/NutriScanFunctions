using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace HealthCheckConnector
{
    public class GetHealthCheckResultTrigger
    {
        //[FunctionName("GetHealthCheckResult")]
        public void Run([BlobTrigger("test/{name}", Connection = "storageConnectionString")]Stream myBlob, string name, ILogger log)
        {
            try
            {
                log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
                AnalyzeDocument(myBlob);
            }
            catch (Exception ex)
            {
                log.LogError($"GetHealthCheckResult Function failed for image :{name}. Error details : {ex.Message + ex.InnerException}");
            }
        }

        private async void AnalyzeDocument(Stream fileStream)
        {
            try
            {
                string endpoint = "https://docintillegent.cognitiveservices.azure.com/";
                string apiKey = "510661ca2a714b7fb39bf574b7cec59a";
                AzureKeyCredential credential = new AzureKeyCredential(apiKey);
                DocumentAnalysisClient client = new DocumentAnalysisClient(new Uri(endpoint), credential);

                string modelId = "medreportAnalyzer";
                //Uri fileUri = new Uri("<fileUri>");

                AnalyzeDocumentOperation operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, modelId, fileStream);
                AnalyzeResult result = operation.Value;

                Console.WriteLine($"Document was analyzed with model with ID: {result.ModelId}");

                foreach (AnalyzedDocument document in result.Documents)
                {
                    Console.WriteLine($"Document of type: {document.DocumentType}");

                    foreach (KeyValuePair<string, DocumentField> fieldKvp in document.Fields)
                    {
                        string fieldName = fieldKvp.Key;
                        DocumentField field = fieldKvp.Value;

                        Console.WriteLine($"Field '{fieldName}': ");

                        Console.WriteLine($"  Content: '{field.Content}'");
                        Console.WriteLine($"  Confidence: '{field.Confidence}'");
                    }
                }

                // Iterate over lines and selection marks on each page
                foreach (DocumentPage page in result.Pages)
                {
                    Console.WriteLine($"Lines found on page {page.PageNumber}");
                    foreach (var line in page.Lines)
                    {
                        Console.WriteLine($"  {line.Content}");
                    }

                    Console.WriteLine($"Selection marks found on page {page.PageNumber}");
                    foreach (var selectionMark in page.SelectionMarks)
                    {
                        Console.WriteLine($"  Selection mark is '{selectionMark.State}' with confidence {selectionMark.Confidence}");
                    }
                }

                // Iterate over the document tables
                for (int i = 0; i < result.Tables.Count; i++)
                {
                    Console.WriteLine($"Table {i + 1}");
                    foreach (var cell in result.Tables[i].Cells)
                    {
                        Console.WriteLine($"  Cell[{cell.RowIndex}][{cell.ColumnIndex}] has content '{cell.Content}' with kind '{cell.Kind}'");
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
