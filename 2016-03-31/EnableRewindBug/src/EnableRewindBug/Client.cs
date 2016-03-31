// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace EnableRewindBug
{
    public class Client
    {
        private const long OneByte = 1;
        private const long OneKilobyte = OneByte * 1024;
        private const long OneMegabyte = OneKilobyte * 1024;
        private const long OneGigabyte = OneMegabyte * 1024;

        private static string _apiEndpoint = "http://localhost:5000/";

        private static readonly Random Random = new Random(DateTime.UtcNow.Millisecond);

        public async Task SendLoad(Func<string, RandomDataStreamContent> contentGenerator, int filesToAdd = 1)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(20);
                using (var form = new MultipartFormDataContent())
                {
                    form.Add(new StringContent("{\"section\" : \"This is a simple JSON content fragment\"}"), "metadata");
                    for (var iter = 0; iter < filesToAdd; ++iter)
                    {
                        var fileName = Guid.NewGuid().ToString().ToLower();
                        var fileContent = contentGenerator(fileName);
                        form.Add(fileContent, "file", fileName);
                    }

                    var response = await client.PostAsync(_apiEndpoint + "api/upload", form);
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorMessage = "Upload failed: " + (int)response.StatusCode + " " + response.ReasonPhrase;
                        throw new Exception(errorMessage + Environment.NewLine + await response.Content.ReadAsStringAsync());
                    }
                }
            }
        }
        
        // Scenario 0: Small text part + large text part: 100KB
        public RandomDataStreamContent Scenario0FileContentGenerator(string fileName)
        {
            return GenerateFileContent(fileName, 100 * OneKilobyte, DataGenerationType.Text);
        }

        // Scenario 1: Small text part + large text part: 10MB/100MB/1GB [5:3:1]
        public RandomDataStreamContent Scenario1FileContentGenerator(string fileName)
        {
            return FiveThreeOneChanceOfTenMegHundredMegOneGig(fileName, DataGenerationType.Text);
        }

        // Scenario 2: Small text part + large binary part: 10MB/100MB/1GB [5:3:1]
        public RandomDataStreamContent Scenario2FileContentGenerator(string fileName)
        {
            return FiveThreeOneChanceOfTenMegHundredMegOneGig(fileName, DataGenerationType.Binary);
        }

        // Scenario 3: A number of large parts: text/binary [2:1] totalling more than 4GB (to test 32-bit limit)
        public RandomDataStreamContent Scenario3FileContentGenerator(string fileName)
        {
            return TwoToOnceChanceOfTextVersusBinary(fileName, OneGigabyte);
        }

        // 10MB/100MB/1GB [5:3:1]
        public RandomDataStreamContent FiveThreeOneChanceOfTenMegHundredMegOneGig(string fileName, DataGenerationType type)
        {
            // We do this by getting a random value between 0 and 8, and then deciding on size:
            // 0 -> 10 MB
            // 1 -> 10 MB
            // 2 -> 10 MB
            // 3 -> 10 MB
            // 4 -> 10 MB
            // 5 -> 100 MB
            // 6 -> 100 MB
            // 7 -> 100 MB
            // 8 -> 1 GB

            long fileSize;
            var selector = Random.Next(0, 8);
            if (selector <= 4)
            {
                fileSize = 10 * OneMegabyte;
            }
            else if (selector < 8)
            {
                fileSize = 100 * OneMegabyte;
            }
            else
            {
                fileSize = OneGigabyte;
            }

            return GenerateFileContent(fileName, fileSize, type);
        }

        // text/binary [2:1]
        public RandomDataStreamContent TwoToOnceChanceOfTextVersusBinary(string fileName, long fileSize)
        {
            // We do this by getting a random value between 0 and 2, and then deciding on type:
            // 0 -> Text
            // 1 -> Text
            // 2 -> Binary

            return GenerateFileContent(fileName, fileSize, Random.Next(0, 2) < 2 ? DataGenerationType.Text : DataGenerationType.Binary);
        }

        public RandomDataStreamContent GenerateFileContent(string fileName, long fileSize, DataGenerationType type)
        {
            var fileContent = new RandomDataStreamContent(type, fileSize, fileName);
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "\"files\"",
                FileName = "\"" + fileName + "\""
            };
            var mediaType = type == DataGenerationType.Binary ? "application/octet-stream" : "text/plain";
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            return fileContent;
        }

        public static void PrintLine(string input, params object[] paramStrings)
        {
            Console.Write($"[{ DateTime.UtcNow.ToString(CultureInfo.InvariantCulture) }] ");
            Console.WriteLine(input, paramStrings);
        }
    }
}
