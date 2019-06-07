using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImageBedDemo.Configs
{
    /// <summary>
    /// 应用程序配置
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public static string ConnString { get; set; }

        /// <summary>
        /// 百度AK
        /// </summary>
        public static string BaiduAK { get; set; }

        /// <summary>
        /// Redis连接字符串
        /// </summary>
        public static string Redis { get; set; }

        public static MongoConfig MongoConfig { get; set; } = new MongoConfig();

        /// <summary>
        /// OSS配置
        /// </summary>
        public static AliOssConfig AliOssConfig { get; set; } = new AliOssConfig();

        /// <summary>
        /// gitlab图床配置
        /// </summary>
        public static GitlabConfig GitlabConfig { get; set; } = new GitlabConfig();

        /// <summary>
        /// 码云图床配置
        /// </summary>
        public static GiteeConfig GiteeConfig { get; set; } = new GiteeConfig();

        /// <summary>
        /// 图床域名
        /// </summary>
        public static List<string> ImgbedDomains { get; set; } = new List<string>();
    }

    public class MongoConfig
    {
        public string Url { get; set; }
        public string Database { get; set; }
    }

    public class AliOssConfig
    {
        public bool Enabled { get; set; }
        public string EndPoint { get; set; }
        public string BucketDomain { get; set; }
        public string AccessKeyId { get; set; }
        public string AccessKeySecret { get; set; }
        public string BucketName { get; set; }
    }

    public class GiteeConfig
    {
        public bool Enabled { get; set; }
        public string ApiUrl { get; set; }
        public string RawUrl { get; set; }
        public string AccessToken { get; set; }
        public string Branch { get; set; }
    }

    public class GitlabConfig : GiteeConfig
    {

    }

}
