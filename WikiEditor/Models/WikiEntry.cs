using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WikiEditor.Models
{
    public class WikiEntry
    {
        public string Name { get; set; }
        public List<Tag> Tags { get; set; }
        public string Content { get; set; }
        public List<Link> Links { get; set; }
        public List<InternalLink> InternalLinks { get; set; }
        public string Navigation { get; set; }

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime ModifiedAt { get; set; }
        public string ModifiedBy { get; set; }

        [JsonIgnore]
        public string Path { get; set; }

        public WikiEntry()
        {
            Tags = new List<Tag>();
            Links = new List<Link>();
        }

        public void SetPath()
        {
            Path = new FileInfo(MainWindow.Instance.WikiPath).Directory + @"\" + Name + ".html";
        }

        public void SaveToFile(bool force = false)
        {
            SetPath();

            var oldContent = "";
            if (File.Exists(Path))
                oldContent = File.ReadAllText(Path);

            if(oldContent != Content || !File.Exists(Path) || force)
            {
                var ToC = MainWindow.Instance.CurrentWiki.TableOfContents.Content;
                var title = MainWindow.Instance.CurrentWiki.Name + " - " + Name;
                var tempContent = InsertHeaderIDs(Content);

                var fileContent = File.ReadAllText(Wiki.HTMLTemplate).Replace("%Content%", tempContent).Replace("%ToC%", ToC).Replace("%Title%", title).Replace("%Nav%",Navigation);
                File.WriteAllText(Path, fileContent);

                if(!force)
                {
                    ModifiedAt = DateTime.Now;
                    ModifiedBy = Helper.GetUser();
                }
            }
        }

        private string InsertHeaderIDs(string content)
        {
            var newContent = content;

            if (string.IsNullOrEmpty(Content) || !Content.Contains("<h"))
                return newContent;

            var matches = Regex.Matches(newContent, "<h[1-6]{1}");
            foreach(Match match in matches)
            {
                newContent.Insert(match.Index, $"id=\"\"");
            }

            return newContent;
        }

        public void ReadLinksFromContent()
        {
            Links = new List<Link>();
            var missingLinks = new List<Link>();

            if (string.IsNullOrEmpty(Content) || !Content.Contains("<a"))
                return;

            SetPath();
            var doc = new HtmlDocument();
            doc.Load(Path);

            var contentNode = doc.GetElementbyId("content");
            var linkList = contentNode.SelectNodes(".//a").ToList();

            foreach(var a in linkList)
            {
                var displayName = a.InnerText;
                var href = a.GetAttributeValue("href", "");

                if(!string.IsNullOrEmpty(displayName) && ! string.IsNullOrEmpty(href))
                {
                    var entryName = href.Split('.')[0];

                    var entry = MainWindow.Instance.CurrentWiki.Entries.Find(e => e.Name == entryName);
                    if (entry != null)
                    {
                        var link = new Link() { DisplayName = displayName, EntryName = entry.Name };

                        if (Links.Find(l => l.EntryName == link.EntryName) == null)
                        {
                            Links.Add(link);
                        }
                    }
                    else
                    {
                        var link = new Link() { DisplayName = displayName, EntryName = entryName };

                        if (missingLinks.Find(l => l.DisplayName == link.DisplayName) == null)
                        {
                            missingLinks.Add(link);
                        }
                    }
                }
            }

            if(missingLinks.Any())
            {
                MainWindow.Instance.MissingLinks = missingLinks;
            }
        }

        public void RenderNavigationFromContent()
        {
            if (string.IsNullOrEmpty(Content) || !Content.Contains("<h"))
                return;

            Navigation = "";
            InternalLinks = new List<InternalLink>();

            SetPath();
            var doc = new HtmlDocument();
            doc.Load(Path);

            var contentNode = doc.GetElementbyId("content");
            string xpathQuery = ".//*[starts-with(name(),'h') and string-length(name()) = 2 and number(substring(name(), 2)) <= 6]";
            var headerList = contentNode.SelectNodes(xpathQuery).ToList();

            foreach (var a in headerList)
            {
                var link = new InternalLink() { HeaderName = a.InnerText, Level = Convert.ToInt32(a.Name.Replace("h", "")) };

                Navigation += $"<li style=\"padding-left: {(link.Level-2)*20}px;\"><a href=\"#{link.HeaderName.Replace(" ","_")}\">{link.HeaderName}</a></li>";

                InternalLinks.Add(link);
            }
        }
    }
}
