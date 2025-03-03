﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Text;
using HtmlAgilityPack;
using ImageCrawler;
using Pamaxie.ImageCrawler.Image_Prep;

namespace Pamaxie.ImageCrawler
{
    public class UrlInteraction
    {
        /// <summary>
        /// Parse links and get a list of them.
        /// </summary>
        /// <param name="urlToCrawl"></param>
        /// <returns></returns>
        public static List<string> ParseLinks(string urlToCrawl)
        {
            try
            {

                string data = string.Empty;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlToCrawl);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Stream receiveStream = response.GetResponseStream();
                    StreamReader readStream = null;

                    if (String.IsNullOrWhiteSpace(response.CharacterSet))
                        readStream = new StreamReader(receiveStream);
                    else
                        readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));

                    data = readStream.ReadToEnd();

                    response.Close();
                    readStream.Close();
                }

                HashSet<string> list = new HashSet<string>();
                if (string.IsNullOrEmpty(data))
                    return new List<string>();

                var doc = new HtmlDocument();
                doc.LoadHtml(data);
                HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//a[@href]");

                foreach (var n in nodes)
                {
                    string href = n.Attributes["href"].Value;
                    var url = GetAbsoluteUrlString(urlToCrawl, href);
                    if (string.IsNullOrEmpty(url) || url.Contains(Program.BaseUrl)) continue;
                    list.Add(url);
                }

                return list.ToList();
            }
            catch (WebException ex)
            {
                Console.WriteLine(ex);
                return new List<string>();
            }

        }

        /// <summary>
        /// Gets the Absulute url from a ref url. If it is not a ref url we assume its different website and ignore it.
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        static string GetAbsoluteUrlString(string baseUrl, string url)
        {
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            if (!uri.IsAbsoluteUri)
                uri = new Uri(new Uri(baseUrl), uri);
            return uri.ToString();
        }

        /// <summary>
        /// Grabs all Images from a url
        /// </summary>
        /// <param name="url"></param>
        public static void GrabAllImages(string url)
        {
            // declare html document
            var document = new HtmlWeb().Load(url);
            // now using LINQ to grab/list all images from website
            var imageUrls = document.DocumentNode.Descendants("img")
                .Select(e => e.GetAttributeValue("src", null))
                .Where(s => !String.IsNullOrEmpty(s)).ToArray();

            if (imageUrls.Length == 0) return;
            // now showing all images from web page one by one
            foreach (var item in imageUrls)
            {
                try
                {
                    if (Program.DownloadedImageUrls.Contains(url))
                    {
                        continue;
                    }

                    var file = ImagePreparation.DownloadFile(item);
                    ImagePreparation.PrepareFile(file?.FullName).MoveTo(Program.ImageDestinationDir + "/" + ImagePreparation.PrepareFile(file?.FullName).Name + "." +
                                                                        ImagePreparation.PrepareFile(file?.FullName).Extension);
                    Program.DownloadedImageUrls.Add(item);
                    Program.CurrentImgCount++;
                }
                catch(Exception ex)
                {

                }

            }
        }
    }
}
