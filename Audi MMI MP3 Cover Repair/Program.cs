// See https://aka.ms/new-console-template for more information
using IdSharp.Tagging.ID3v2;
using IdSharp.Tagging.ID3v2.Frames;
using System.Drawing;

foreach (var item in args)
{
#if DEBUG
    Console.WriteLine("参数: {0}", item);
#endif
    ID3v2Tag id3v2 = new ID3v2Tag(item);
    Console.WriteLine(string.Format("Title:     {0}", id3v2.Title));
    Console.WriteLine(string.Format("Pictures:  {0}", id3v2.PictureList.Count));
    IAttachedPicture attachedPicture = id3v2.PictureList[0];
    
    //保存封面图片
    FileStream fileStream = new FileStream(attachedPicture.Description + "." + attachedPicture.PictureExtension, FileMode.OpenOrCreate, FileAccess.Write);
    fileStream.Write(attachedPicture.PictureData);

    //将封面加载到内存
    Stream stream = new MemoryStream(attachedPicture.PictureData);
    Image image = Image.FromStream(stream);
    
    Console.WriteLine(string.Format("Picture Type:     {0}", attachedPicture.PictureType));
    Console.WriteLine(string.Format("Size:     {0}x{1}", image.Size.Width, image.Size.Height));
    
}