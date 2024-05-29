using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Xml;

namespace WikiEditor.Models
{
    public class Wiki
    {
        public string Name { get; set; }
        public List<Tag> Tags { get; set; }
        public List<WikiEntry> Entries { get; set; }
        public WikiEntry HomeEntry { get; set; }
        public WikiEntry TableOfContents { get; set; }

        public static string HTMLTemplate { get; set; }
        public static string ToCHTMLTemplate { get; set; }

        public Wiki(){}

        public static Wiki ReadWiki(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<Wiki>(json);
        }

        public static void WriteWiki(string filePath, Wiki wiki, bool force = false)
        {
            wiki.UpdateToC();
            wiki.HomeEntry.SaveToFile();

            foreach (var entry in wiki.Entries)
            {
                entry.SaveToFile(force);
                entry.ReadLinksFromContent();
                entry.RenderNavigationFromContent();
            }

            string jsonString = JsonConvert.SerializeObject(wiki, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, jsonString);
        }

        public void UpdateToC()
        {
            var toC = "";

            foreach (var tag in Tags.OrderBy(t => t.Name))
            {
                var entries = Entries.Where(e => e.Tags.Find(t => t.Name == tag.Name) != null).OrderBy(e=> e.Name);
                if (tag.Name == Tag.Unknown.Name && !entries.Any())
                    continue;

                toC += $"<li><h4>{tag.Name}</h4>\r\n{Helper.GetTabs(6)}<ul>";

                foreach (var entry in entries)
                {
                    toC += $"\r\n{Helper.GetTabs(7)}<li><h5><a href=\"{entry.Name}.html\">{entry.Name}</a></h5></li>";
                }

                toC += $"\r\n{Helper.GetTabs(6)}</ul>\r\n{Helper.GetTabs(5)}</li>";
            }

            TableOfContents.Content = File.ReadAllText(Wiki.ToCHTMLTemplate).Replace("%List%", toC);
        }
    }
}
