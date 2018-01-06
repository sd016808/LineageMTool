using Imgur.API;
using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using Imgur.API.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace LineageMTool
{
    class LineMessage
    {
        public Action<string> ErrorCallBack;
        public void SendMessageToLine(string uid, string message, Image image)
        {
            try
            {
                isRock.LineBot.Utility.PushMessage(
                uid, message, "XfzPgOG9PcPqQj38QNOWkAtpSC8M7K2TJGPe0erfeogRIOr/6Xh5Hdl+CDwt0KUgkd0PvTLQ5ebqCyzYNT9kbJshTDy54NgKZG/9tFaRTQPmWH4x/l7xpGXTWTdLSdLVx9aKtSYvLVFJoUd0vPbfvAdB04t89/1O/w1cDnyilFU=");
                string imageUrl = UploadImage(image);
                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    Task.Run(new Action(() =>
                    {
                        isRock.LineBot.Utility.PushImageMessage(
                        uid, imageUrl, imageUrl, "XfzPgOG9PcPqQj38QNOWkAtpSC8M7K2TJGPe0erfeogRIOr/6Xh5Hdl+CDwt0KUgkd0PvTLQ5ebqCyzYNT9kbJshTDy54NgKZG/9tFaRTQPmWH4x/l7xpGXTWTdLSdLVx9aKtSYvLVFJoUd0vPbfvAdB04t89/1O/w1cDnyilFU=");
                    }));
                }
            }
            catch
            {
                ErrorCallBack?.Invoke("請確認Line uid設定正確");
            }
        }
        private string UploadImage(Image gameImage)
        {
            try
            {
                var client = new ImgurClient("6ef47358e9c4197", "4803d40aec622afd20d5409696a624c13fcc716e");
                var endpoint = new ImageEndpoint(client);
                IImage image;
                gameImage.Save("screenCapture.jpg", ImageFormat.Jpeg);
                using (var fs = new FileStream("screenCapture.jpg", FileMode.Open))
                {
                    image = endpoint.UploadImageStreamAsync(fs).GetAwaiter().GetResult();
                }
                return image.Link;
            }
            catch (ImgurException imgurEx)
            {
                Debug.Write("An error occurred uploading an image to Imgur.");
                Debug.Write(imgurEx.Message);
                return string.Empty;
            }
        }
        private string DownloadImage(int imageId)
        {
            try
            {
                var client = new ImgurClient("a7d89efd3720246", "f67e11527f51f7dae687900806901fed26b07e42");
                var endpoint = new ImageEndpoint(client);
                var image = endpoint.GetImageAsync("IMAGE_ID").GetAwaiter().GetResult();
                Debug.Write("Image retrieved. Image Url: " + image.Link);
                return image.Link;
            }
            catch (ImgurException imgurEx)
            {
                Debug.Write("An error occurred getting an image from Imgur.");
                Debug.Write(imgurEx.Message);
                return string.Empty;
            }
        }
    }
}
