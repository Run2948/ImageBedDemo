﻿using Aliyun.OSS;
using ImageBedDemo.Configs;
using Newtonsoft.Json.Linq;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

namespace ImageBedDemo.Common
{
    /// <summary>
    /// 图床客户端
    /// </summary>
    public class ImagebedClient
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// 图床客户端
        /// </summary>
        /// <param name="httpClient"></param>
        public ImagebedClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// OSS客户端
        /// </summary>
        public static OssClient OssClient { get; set; } = new OssClient(AppConfig.AliOssConfig.EndPoint, AppConfig.AliOssConfig.AccessKeyId, AppConfig.AliOssConfig.AccessKeySecret);

        /// <summary>
        /// 上传图片
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task<(string url, bool success)> UploadImage(Stream stream, string file)
        {
            if (AppConfig.GiteeConfig.Enabled)
            {
                return await UploadGitee(stream, file);
            }

            if (AppConfig.GitlabConfig.Enabled)
            {
                return await UploadGitlab(stream, file);
            }

            if (AppConfig.AliOssConfig.Enabled)
            {
                return await UploadOss(stream, file);
            }

            return await UploadSmms(stream, file);
        }

        /// <summary>
        /// 码云图床
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task<(string url, bool success)> UploadGitee(Stream stream, string file)
        {
            string base64String = Convert.ToBase64String(stream.StreamToByte());
            string path = $"{DateTime.Now:yyyyMMdd}/{Path.GetFileName(file)}";
            using (var resp = await _httpClient.PostAsJsonAsync(AppConfig.GiteeConfig.ApiUrl + HttpUtility.UrlEncode(path), new
            {
                access_token = AppConfig.GiteeConfig.AccessToken,
                content = base64String,
                message = "上传一张图片"
            }))
            {
                if (resp.IsSuccessStatusCode || (await resp.Content.ReadAsStringAsync()).Contains("already exists"))
                {
                    return (AppConfig.GiteeConfig.RawUrl + path, true);
                }
            }

            return await UploadSmms(stream, file);
            //return await Task.Run(() => ("", false));
        }

        /// <summary>
        /// gitlab图床
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task<(string url, bool success)> UploadGitlab(Stream stream, string file)
        {
            string base64String = Convert.ToBase64String(stream.StreamToByte());
            _httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", AppConfig.GitlabConfig.AccessToken);
            string path = $"{DateTime.Now:yyyyMMdd}/{Path.GetFileName(file)}";
            using (var resp = await _httpClient.PostAsJsonAsync(AppConfig.GitlabConfig.ApiUrl + HttpUtility.UrlEncode(path), new
            {
                branch = AppConfig.GitlabConfig.Branch,
                author_email = "1@1.cn",
                author_name = "ldqk",
                encoding = "base64",
                content = base64String,
                commit_message = "上传一张图片"
            }))
            {
                if (resp.IsSuccessStatusCode || (await resp.Content.ReadAsStringAsync()).Contains("already exists"))
                {
                    return (AppConfig.GitlabConfig.RawUrl + path, true);
                }
            }

            return await UploadSmms(stream, file);
        }

        /// <summary>
        /// 阿里云Oss图床
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task<(string url, bool success)> UploadOss(Stream stream, string file)
        {
            var objectName = DateTime.Now.ToString("yyyyMMdd") + "/" + Guid.NewGuid().ToString("N") + Path.GetExtension(file);
            var result = Policy.Handle<Exception>().Retry(5, (e, i) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                Console.ResetColor();
            }).Execute(() => OssClient.PutObject(AppConfig.AliOssConfig.BucketName, objectName, stream));
            return result.HttpStatusCode == HttpStatusCode.OK ? (AppConfig.AliOssConfig.BucketDomain + "/" + objectName, true) : await UploadSmms(stream, file);
        }

        /// <summary>
        /// 上传图片到sm图床
        /// </summary>
        /// <returns></returns>
        public async Task<(string url, bool success)> UploadSmms(Stream stream, string file)
        {
            string url = string.Empty;
            bool success = false;
            _httpClient.DefaultRequestHeaders.UserAgent.Add(ProductInfoHeaderValue.Parse("Mozilla/5.0"));
            using (var bc = new StreamContent(stream))
            {
                bc.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = Path.GetFileName(file),
                    Name = "smfile"
                };
                using (var content = new MultipartFormDataContent { bc })
                {
                    var code = await _httpClient.PostAsync("https://sm.ms/api/upload?inajax=1&ssl=1", content).ContinueWith(t =>
                    {
                        if (t.IsCanceled || t.IsFaulted)
                        {
                            return 0;
                        }

                        var res = t.Result;
                        if (res.IsSuccessStatusCode)
                        {
                            try
                            {
                                string s = res.Content.ReadAsStringAsync().Result;
                                var token = JObject.Parse(s);
                                url = (string)token["data"]["url"];
                                return 1;
                            }
                            catch
                            {
                                return 2;
                            }
                        }

                        return 2;
                    });
                    if (code == 1)
                    {
                        success = true;
                    }
                }
            }

            return success ? (url, true) : await UploadPeople(stream, file);
        }

        /// <summary>
        /// 上传图片到人民网图床
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task<(string url, bool success)> UploadPeople(Stream stream, string file)
        {
            bool success = false;
            _httpClient.DefaultRequestHeaders.UserAgent.Add(ProductInfoHeaderValue.Parse("Chrome/72.0.3626.96"));
            using (var sc = new StreamContent(stream))
            {
                using (var mc = new MultipartFormDataContent
                {
                    { sc, "Filedata", Path.GetFileName(file) },
                    {new StringContent("."+Path.GetExtension(file)),"filetype"}
                })
                {
                    var str = await _httpClient.PostAsync("http://bbs1.people.com.cn/postImageUpload.do", mc).ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            var res = t.Result;
                            if (res.IsSuccessStatusCode)
                            {
                                string result = res.Content.ReadAsStringAsync().Result;
                                string url = "http://bbs1.people.com.cn" + (string)JObject.Parse(result)["imageUrl"];
                                if (url.EndsWith(Path.GetExtension(file)))
                                {
                                    success = true;
                                    return url;
                                }
                            }
                        }

                        return "";
                    });
                    return (str, success);
                }
            }
        }
    }
}
