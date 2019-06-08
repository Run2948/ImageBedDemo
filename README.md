# .NET CORE上传文件到码云仓库【搭建自己的图床】

> 先建一个公共仓库(随意提交一个README文件或者.gitignore文件保证master分支的存在)，然后到gitee的个人设置页面找到【私人令牌】菜单创建一个access_token。Gitee官方还友好的提供了基于swagger的API文档和调试页面： https://gitee.com/api/v5/swagger#/postV5ReposOwnerRepoContentsPath

## 搭建步骤

#### 1.新建一个名为`imagebed`的公有仓库
* 码云正常建库步骤即可
#### 2.为`imagebed`仓库创建`master`主分支
* [推荐]可以从本地向仓库随意提交一个README文件或.gitignore文件
* 或者你用自己的方式也行，只要保证仓库具有一个`master`分支即可
#### 3.到个人设置页面找到【私人令牌】生成新令牌
* 找到【私人令牌】
[![找到【私人令牌】](https://i.loli.net/2019/06/07/5cfa213058bcf83954.png)](https://i.loli.net/2019/06/07/5cfa213058bcf83954.png)
* 生成新令牌
[![生成新令牌](https://i.loli.net/2019/06/07/5cfa21307e75743876.png)](https://i.loli.net/2019/06/07/5cfa21307e75743876.png)
#### 4.使用Gitee官网API文档简单测试文件上传
* 填写信息
[![填写信息](https://i.loli.net/2019/06/07/5cfa26b42b48b17579.png)](https://i.loli.net/2019/06/07/5cfa26b42b48b17579.png)
* 点击测试
[![点击测试](https://i.loli.net/2019/06/07/5cfa26b40998762816.png)](https://i.loli.net/2019/06/07/5cfa26b40998762816.png)
* 提交记录
[![提交记录](https://i.loli.net/2019/06/07/5cfa26b400b0860060.png)](https://i.loli.net/2019/06/07/5cfa26b400b0860060.png)
* 查看内容
[![查看内容](https://i.loli.net/2019/06/07/5cfa26b3f202998601.png)](https://i.loli.net/2019/06/07/5cfa26b3f202998601.png)
## 使用方法
> 基于.NET CORE MVC项目实现
```
	/// <summary>
	/// 码云仓储文件上传API
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
			message = "上传一个文件"
		}))
		{
			if (resp.IsSuccessStatusCode || (await resp.Content.ReadAsStringAsync()).Contains("already exists"))
			{
				return (AppConfig.GiteeConfig.RawUrl + path, true);
			}
		}

		return await Task.Run(() => (null, false));
	}

    /// <summary>
    /// MVC上传文件
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    [HttpPost("upload"), ApiExplorerSettings(IgnoreApi = false)]
    public async Task<ActionResult> UploadFile(IFormFile file)
    {
        var (url, success) = await _imagebedClient.UploadImage(file.OpenReadStream(), file.FileName);
        return await success ? Json(new { code = 1, msg = "success", data = url }) : Json(new { code = 0, msg = "failure" });            
    }
```

