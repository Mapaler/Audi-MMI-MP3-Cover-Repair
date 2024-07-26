奥迪汽车多媒体交互系统 MP3 音乐文件封面修复
=======
## 功能
网易云音乐下的歌封面又大，位置又不是封面，就会导致车上放不起。

将 MP3 文件的图片全部缩小到不大于 480x480 ，位置调整到封面(Cover-Front)。

![预览图](doc/preview.webp)

## 运行
查看帮助信息获取运行方式
```bat
AMMICP.exe -h
```

示例
```bat
AMMICP.exe C:\music\a.mp3 "D:\music\good song.mp3" E:\a.mp3
```

## 使用开源库
* [System.CommandLine](https://www.nuget.org/packages/System.CommandLine) - .NET CommandLine parser
* [Audio Tools Library (ATL) for .NET](https://www.nuget.org/packages/z440.atl.core/) - .NET ID3 Tagging Library