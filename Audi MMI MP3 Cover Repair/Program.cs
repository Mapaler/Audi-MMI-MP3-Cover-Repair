// See https://aka.ms/new-console-template for more information
using IdSharp.Tagging.ID3v1;
using IdSharp.Tagging.ID3v2;
using IdSharp.Tagging.ID3v2.Frames;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

const uint maxWidth = 480; //图象的最大宽度与高度
const uint maxHeight = 480;
const byte quality = 90; //JPEG的图像质量

//获取JPEG的编码器信息
ImageCodecInfo? jpegEncoder = null;
foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageDecoders())
{
    if (codec.FormatID == ImageFormat.Jpeg.Guid)
    {
        jpegEncoder = codec;
        break;
    }
}

foreach (var item in args)
{
    Debug.WriteLine("参数: {0}", item);

    FileInfo musicFile = new(item); //获取目标文件路径信息
    if (!musicFile.Exists) //如果音乐文件不存在则跳过
    {
        Console.Error.WriteLine("错误：未发现音乐文件 {0}", item);
        continue;
    }
    ID3v2Tag id3v2 = new(item);

    Console.WriteLine(string.Format("Title:     {0}", id3v2.Title));
    Console.WriteLine(string.Format("Pictures:  {0}", id3v2.PictureList.Count));
    IAttachedPicture attachedPicture = id3v2.PictureList[0];

#if DEBUG
    //保存封面图片
    string coverFileName = musicFile.DirectoryName + "/" + Path.GetFileNameWithoutExtension(musicFile.Name) + "-" + attachedPicture.PictureType + "." + attachedPicture.PictureExtension;
    FileStream fileStream = new(coverFileName, FileMode.OpenOrCreate, FileAccess.Write);
    fileStream.Write(attachedPicture.PictureData);
#endif

    //如果不是封面，则清空后重新添加封面
    if (attachedPicture.PictureType != PictureType.CoverFront)
    {
        id3v2.PictureList.Clear();
        attachedPicture.PictureType = PictureType.CoverFront;
        id3v2.PictureList.Add(attachedPicture);
    }

    //将封面加载到内存
    Stream stream = new MemoryStream(attachedPicture.PictureData);
    Image image = Image.FromStream(stream);
    
    Console.WriteLine(string.Format("Picture Type:     {0}", attachedPicture.PictureType));
    Console.WriteLine(string.Format("Size:     {0}x{1}", image.Size.Width, image.Size.Height));
    if (image.Size.Width > maxWidth || image.Size.Height > maxHeight)
    {
        double scale;
        scale = Math.Min((double)maxWidth / (double)image.Size.Width, (double)maxHeight / (double)image.Size.Height);
        Console.WriteLine(string.Format("缩小比例:     {0}", scale));
        Size size = new(Convert.ToInt32(image.Size.Width * scale), Convert.ToInt32(image.Size.Height * scale));
        Image? imgOutput = image.GetThumbnailImage(size.Width, size.Height, null, IntPtr.Zero);

        //创建JPEG压缩的参数
        EncoderParameters parameters = new(1);
        parameters.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality);

        //在内存中压缩转存一遍
        MemoryStream ms = new();
        imgOutput.Save(ms, jpegEncoder, parameters);
        Image imgStream = Image.FromStream(ms);

#if DEBUG
        //保存封面图片
        string coverThumbnailFileName = musicFile.DirectoryName + "/" + Path.GetFileNameWithoutExtension(musicFile.Name) + "-" + attachedPicture.PictureType + "-resize." + attachedPicture.PictureExtension;
        imgStream.Save(coverThumbnailFileName);
        //复制一份新的mp3来修改封面
        string newFilename = musicFile.DirectoryName + "/" + Path.GetFileNameWithoutExtension(musicFile.Name) + "_id3" + musicFile.Extension;
        File.Copy(item, newFilename, true);
#else
        string newFilename = musicFile.FullName;
#endif
        attachedPicture.Picture = imgStream; //将ID3封面修改为新的图片

        id3v2.Save(newFilename);
    }
    else
    {
        Console.WriteLine("图片大小不需要调整");
        continue;
    }
}