using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace TestChunkedUpload.Test
{
    public class TestUpload
    {
        private const string UploadUrl = "https://localhost:44368/api/upload";
        private const string DataFolder = "Data";
        private const string HashUrl = "https://localhost:44368/api/upload/hash?fileName=";

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task Upload_HappyPath()
        {

            var expectedHash = CalculateMD5(Path.Combine(DataFolder, "testo.txt"));

            (await Upload(Path.Combine(DataFolder, "testo-1.txt"), "testo.txt", 0, 3)).Should().BeTrue();
            (await Upload(Path.Combine(DataFolder, "testo-2.txt"), "testo.txt", 1, 3)).Should().BeTrue();
            (await Upload(Path.Combine(DataFolder, "testo-3.txt"), "testo.txt", 2, 3)).Should().BeTrue();

            var hash = GetHash("testo.txt");

            hash.Should().Be(expectedHash);
        }

        [Test]
        public async Task Upload_WrongOrder_Ok()
        {

            var expectedHash = CalculateMD5(Path.Combine(DataFolder, "testo.txt"));

            (await Upload(Path.Combine(DataFolder, "testo-2.txt"), "testo.txt", 1, 3)).Should().BeTrue();
            (await Upload(Path.Combine(DataFolder, "testo-3.txt"), "testo.txt", 2, 3)).Should().BeTrue();
            (await Upload(Path.Combine(DataFolder, "testo-1.txt"), "testo.txt", 0, 3)).Should().BeTrue();

            var hash = GetHash("testo.txt");

            hash.Should().Be(expectedHash);
        }

        [Test]
        public void Upload_HappyPathParallel()
        {

            var expectedHash = CalculateMD5(Path.Combine(DataFolder, "testo.txt"));

            var t1 = Upload(Path.Combine(DataFolder, "testo-1.txt"), "testo.txt", 0, 3);
            var t2 = Upload(Path.Combine(DataFolder, "testo-2.txt"), "testo.txt", 1, 3);
            var t3 = Upload(Path.Combine(DataFolder, "testo-3.txt"), "testo.txt", 2, 3);

            Task.WaitAll(t1, t2, t3);

            var hash = GetHash("testo.txt");

            hash.Should().Be(expectedHash);
        }

        [Test]
        public void Upload_HappyPathParallelBruteForce()
        {

            var expectedHash = CalculateMD5(Path.Combine(DataFolder, "testo.txt"));

            Parallel.For(0, 100, (index, state) =>
            {
                var t1 = Upload(Path.Combine(DataFolder, "testo-1.txt"), $"testo{index}.txt", 0, 3);
                var t2 = Upload(Path.Combine(DataFolder, "testo-2.txt"), $"testo{index}.txt", 1, 3);
                var t3 = Upload(Path.Combine(DataFolder, "testo-3.txt"), $"testo{index}.txt", 2, 3);

                Task.WaitAll(t1, t2, t3);
            });

            for (var n = 0; n < 100; n++)
            {
                var hash = GetHash($"testo{n}.txt");

                hash.Should().Be(expectedHash);
            }
        }

        public static async Task<bool> Upload(string filePath, string fileName, int index, int totalCount)
        {
            using var client = new HttpClient();

            using var content =
                new MultipartFormDataContent("Upload----" + DateTime.Now.ToString(CultureInfo.InvariantCulture))
                {
                    {new StreamContent(File.OpenRead(filePath)), "file", fileName},
                    {new StringContent(index.ToString()), "index"},
                    {new StringContent(totalCount.ToString()), "totalCount"}
                };


            using var message = await client.PostAsync(UploadUrl, content);

            return message.StatusCode == HttpStatusCode.OK;

        }

        public static string GetHash(string fileName)
        {
            var client = new WebClient();
            return client.DownloadString(HashUrl + fileName);
        }

        static string CalculateMD5(string filename)
        {
            using var md5 = MD5.Create();
            using var stream = System.IO.File.OpenRead(filename);

            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }


    }
}