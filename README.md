奥迪汽车多媒体交互系统 MP3 音乐文件封面修复<br>Audi Multi Media Interface MP3 Cover Repair
=======
我的奥迪汽车是 A3 2014年款，多媒体系统支持 MP3 和 M4A 音频。

但是需要显示封面图片需要具备以下条件：
* MP3 格式文件
* 元数据格式版本为 ID3v2.3
* 图片位置必须位于专辑封面(Cover-Front)
* 图片尺寸 ≤ 499 × 499

以上是我自己发现的，另外[此文章](https://www.petenetlive.com/KB/Article/0001116)还提到文件大小需小于 254kb

翻出来我以前拍的一张手册的照片，显示了媒体支持的格式。  
![手册图](doc/manual.jpg)

网易云音乐下的歌封面图片尺寸过大了，同时图片类型不是封面，就导致在奥迪车上不显示封面画面。

此程序可以将 MP3 文件内嵌的第一张图片默认缩小到不大于 480x480，格式转化为 JPG ，位置调整到专辑封面，元数据版本设置为 ID3v2.3。这样处理后，奥迪汽车多媒体系统就能正常显示封面了。

![预览图](doc/preview.webp)

## 运行
使用 [System.Drawing.Common](https://learn.microsoft.com/dotnet/core/compatibility/core-libraries/6.0/system-drawing-common-windows-only) 处理图片，因此仅支持 Window。
### v2.3 版本
安装 .Net 8 运行时

示例
```bat
AMMICP.exe C:\music\a.mp3 "D:\music\good song.mp3" E:\a.mp3
```

也可以递归处理文件夹
```bat
AMMICP.exe "C:\music folder"
```

查看帮助信息可以获取更多可设置参数
```bat
AMMICP.exe -h
```

## 使用开源库
* [System.CommandLine](https://www.nuget.org/packages/System.CommandLine) - .NET CommandLine parser
* [Audio Tools Library (ATL) for .NET](https://www.nuget.org/packages/z440.atl.core/) - .NET ID3 Tagging Library