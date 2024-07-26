奥迪汽车多媒体交互系统 MP3 音乐文件封面修复
=======
网易云音乐下的歌封面尺寸过大，同时图片类型不是封面，就导致在奥迪车上不显示封面画面。

此程序可以将 MP3 文件内嵌的第一张图片缩小到不大于 480x480 ，位置调整到封面(Cover-Front)，使奥迪汽车多媒体系统能正常显示封面。

![预览图](doc/preview.webp)

## 运行
使用 [System.Drawing.Common](https://learn.microsoft.com/dotnet/core/compatibility/core-libraries/6.0/system-drawing-common-windows-only) 处理图片，因此仅支持 Window。
### v2.x 版本
安装 .Net 8 运行时

示例
```bat
AMMICP.exe C:\music\a.mp3 "D:\music\good song.mp3" E:\a.mp3
```

可以查看帮助信息获取更多参数
```bat
AMMICP.exe -h
```

### v1.x 已废弃

## 使用开源库
* [System.CommandLine](https://www.nuget.org/packages/System.CommandLine) - .NET CommandLine parser
* [Audio Tools Library (ATL) for .NET](https://www.nuget.org/packages/z440.atl.core/) - .NET ID3 Tagging Library