using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using WikiEditor.Models;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace WikiEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }

        public string WikiPath { get; set; }
        public Wiki CurrentWiki { get; set; }
        public WikiEntry CurrentEntry { get; set; }

        public List<Link> MissingLinks { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;

            Instance = this;

            if (Environment.GetCommandLineArgs().Length > 1)
            {
                WikiPath = Environment.GetCommandLineArgs()[1];
            }

            if (string.IsNullOrEmpty(WikiPath))
            {
                var openFileDialog = new OpenFileDialog()
                {
                    Title = "Wiki öffnen",
                    Filter = "Json files (*.json)|*.json|Text files (*.txt)|*.txt",
                    RestoreDirectory = true
                };
                if (openFileDialog.ShowDialog() == true)
                    WikiPath = openFileDialog.FileName;
            }

            if (!string.IsNullOrEmpty(WikiPath) && File.Exists(WikiPath))
                CurrentWiki = Wiki.ReadWiki(WikiPath);
            else
            {
                WikiPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Wiki.json";
                CurrentWiki = new Wiki();
                Wiki.WriteWiki(WikiPath, CurrentWiki);

                MessageBox.Show("Neues Wiki wurde unter " + WikiPath + " erstellt.");
            }

            CurrentWiki.HomeEntry.SetPath();
            foreach (var entry in CurrentWiki.Entries)
            {
                entry.SetPath();
            }

            MissingLinks = new List<Link>();

            Wiki.HTMLTemplate = new FileInfo(WikiPath).Directory + @"\Template.html";
            Wiki.ToCHTMLTemplate = new FileInfo(WikiPath).Directory + @"\Template_ToC.html";

            RenderContent();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
            {
                Save();
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.L)
            {
                var pos = TB_Content.CaretIndex;

                var selectionStart = TB_Content.SelectionStart;
                var selectionLength = TB_Content.SelectionLength;

                if(selectionStart > -1 && selectionLength > 0)
                {
                    var selectionText = TB_Content.Text.Substring(selectionStart, selectionLength);
                    var newText = TB_Content.Text.Remove(selectionStart, selectionLength);
                    newText = newText.Insert(selectionStart, $"<a href=\"{selectionText}.html\">{selectionText}</a>");
                    TB_Content.Text = newText;
                }
                else
                {
                    var newText = TB_Content.Text.Insert(pos, "<a href=\".html\"></a>");
                    TB_Content.Text = newText;
                }

                TB_Content.CaretIndex = pos;

                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.I)
            {
                var pos = TB_Content.CaretIndex;

                var selectionStart = TB_Content.SelectionStart;
                var selectionLength = TB_Content.SelectionLength;

                if (selectionStart > -1 && selectionLength > 0)
                {
                    var selectionText = TB_Content.Text.Substring(selectionStart, selectionLength);
                    var newText = TB_Content.Text.Remove(selectionStart, selectionLength);
                    newText = newText.Insert(selectionStart, $"<i>{selectionText}</i>");
                    TB_Content.Text = newText;
                }
                else
                {
                    var newText = TB_Content.Text.Insert(pos, "<i></i>");
                    TB_Content.Text = newText;
                }

                TB_Content.CaretIndex = pos;

                e.Handled = true;
            }
        }

        public void RenderContent()
        {
            LB_WikiName.Content = CurrentWiki.Name;

            if (CurrentEntry == null)
            {
                CurrentEntry = CurrentWiki.HomeEntry;
            }

            //Content
            LoadEntry(CurrentEntry.Name);
            CB_AssignTag.ItemsSource = CurrentWiki.Tags.OrderBy(t => t.Name);

            //TableOfContents
            CreateTableOfContent();
            CB_NewEntry_Tag.ItemsSource = CurrentWiki.Tags.OrderBy(t => t.Name);

            //Tags
            Tag_Data.ItemsSource = CurrentWiki.Tags.OrderBy(t=> t.Name);
        }


        public void LoadEntry(string entryName)
        {
            var entry = CurrentWiki.Entries.Find(e => e.Name == entryName);
            if (entry == null)
                entry = CurrentWiki.HomeEntry;

            CurrentEntry = entry;

            if (entry != null)
            {
                entry.ReadLinksFromContent();
                entry.RenderNavigationFromContent();
                LB_Entry.Content = entry.Name;
                TB_Content.Text = entry.Content;
                LB_EntryCreated.Content = $"Erstellt bei {entry.CreatedBy} um {entry.CreatedAt.ToString("HH:mm:ss dd.MM.yyyy")}";
                LB_EntryModified.Content = $"Geändert bei {entry.ModifiedBy} um {entry.ModifiedAt.ToString("HH:mm:ss dd.MM.yyyy")}";

                //Navigation
                CreateNavigation(entry);

                //Tags
                Tags.ItemsSource = entry.Tags.OrderBy(t => t.Name);

                //Links
                Links.ItemsSource = entry.Links.OrderBy(l => l.DisplayName);
                LinksMissing.ItemsSource = MissingLinks.OrderBy(l => l.DisplayName);
            }
        }

        public void CreateNavigation(WikiEntry entry)
        {
            Navigations_Stack.Children.Clear();

            foreach(var link in entry.InternalLinks)
            {
                var button = new Button ()
                {
                    Content = link.HeaderName,
                    Margin = new Thickness(link.Level == 1?10:(link.Level-2)*40,10,10,0),
                    Style = (Style)FindResource("Hyperlink"),
                    FontSize = 18,
                    MaxWidth = 300
                };
                button.Click += new RoutedEventHandler(BTN_NavigateToHeader_Click);

                Navigations_Stack.Children.Add(button);
            }
        }

        public void CreateTableOfContent()
        {
            TableOfContents_Stack.Children.Clear();
            var tagStacks = new Dictionary<string, StackPanel>();

            foreach (var tag in CurrentWiki.Tags.OrderBy(t => t.Name))
            {
                var tagExpander = new Expander()
                {
                    Name = "Expander" + tag.Name,
                    Header = tag.Name,
                    IsExpanded = true,
                    Background = new SolidColorBrush(tag.Color),
                    FontSize=20,
                    Padding=new Thickness(10,10,10,0)
                };
                var tagStack = new StackPanel() { Margin= new Thickness(10,0,10,30) };

                foreach (var entry in CurrentWiki.Entries.Where(e => e.Tags.Find(t => t.Name == tag.Name) != null).OrderBy(e => e.Name))
                {
                    var entryButton = new Button()
                    {
                        Name = entry.Name.Replace(" ","_").Replace("(","_").Replace(")","_").Replace("-","_"),
                        Content = entry.Name,
                        Style = (Style)FindResource("Hyperlink"),
                        Margin= new Thickness(10,10,10,0),
                        Background = Brushes.WhiteSmoke,
                        FontSize= 18,
                        MaxWidth=300
                    };
                    entryButton.Click += new RoutedEventHandler(BTN_OpenEntry_Click);

                    tagStack.Children.Add(entryButton);
                }

                tagExpander.Content = tagStack;
                TableOfContents_Stack.Children.Add(tagExpander);
            }
        }

        private void BTN_NavigateToHeader_Click(object sender, RoutedEventArgs e)
        {
            var targetPoint = TB_Content.Text.IndexOf($">{((Button)sender).Content}</h");
            
            if(targetPoint > -1)
            {
                TB_Content.Focus();
                TB_Content.CaretIndex = targetPoint+1;
            }
        }

        private void BTN_SaveAll_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        public void Save()
        {
            CurrentEntry.Content = TB_Content.Text;
            CurrentWiki.UpdateToC();

            Wiki.WriteWiki(WikiPath, CurrentWiki);
            MessageBox.Show("Alle Änderungen wurden gespeichert.");

            CurrentWiki = Wiki.ReadWiki(WikiPath);
            RenderContent();
        }

        private void BTN_OpenEntry_Click(object sender, RoutedEventArgs e)
        {
            var entryName = ((Button)sender).Content.ToString();
            LoadEntry(entryName);
        }

        private void BTN_OpenLink_Click(object sender, RoutedEventArgs e)
        {
            var entryName = ((Button)sender).ToolTip.ToString();
            LoadEntry(entryName);
        }

        private void BTN_Add_Tag_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TB_NewTag.Text))
            {
                var color = Colors.White;
                if (CP_NewTag.SelectedColor != null)
                    color = (Color)CP_NewTag.SelectedColor;

                var newTag = new Tag() { Name = TB_NewTag.Text, Color = color };
                CurrentWiki.Tags.Add(newTag);
                Wiki.WriteWiki(WikiPath, CurrentWiki);

                TB_NewTag.Text = "";

                MessageBox.Show("Neues Tag '" + newTag.Name + "' erstellt.");
                RenderContent();
            }
        }

        private void BTN_Add_Entry_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TB_NewEntry.Text))
            {
                CreateEntry(TB_NewEntry.Text, ((Tag)CB_NewEntry_Tag.SelectedValue).Name);
            }

            TB_NewEntry.Text = "";
        }

        public void CreateEntry(string name, string tagName)
        {
            var targetEntry = CurrentWiki.Entries.Find(e => e.Name == name);
            if (targetEntry != null)
            {
                MessageBox.Show("Eintrag existiert bereits!");
                return;
            }

            var newEntry = new WikiEntry()
            {
                Name = TB_NewEntry.Text,
                CreatedAt = DateTime.Now,
                CreatedBy = Helper.GetUser(),
                ModifiedAt = DateTime.Now,
                ModifiedBy = Helper.GetUser()
            };
            var targetTag = CurrentWiki.Tags.Find(t => t.Name == tagName);
            if (targetTag == null)
            {
                if (CurrentWiki.Tags.Find(t => t.Name == "Unbekannt") == null)
                    CurrentWiki.Tags.Add(Models.Tag.Unknown);
                targetTag = Models.Tag.Unknown;
            }
            else newEntry.Tags.Add(targetTag);

            CurrentWiki.Entries.Add(newEntry);
            Wiki.WriteWiki(WikiPath, CurrentWiki);

            MessageBox.Show("Neuer Eintrag '" + newEntry.Name + "' erstellt.");
            MissingLinks = new List<Link>();
            RenderContent();
        }

        private void Tag_Data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selection = ((DataGrid)sender).SelectedItems;
            if (selection == null || selection.Count < 1)
            {
                BTN_Delete_Tag.IsEnabled = false;
            }
            else
            {
                BTN_Delete_Tag.IsEnabled = true;
            }
        }

        private void BTN_Delete_Tag_Click(object sender, RoutedEventArgs e)
        {
            var selection = Tag_Data.SelectedItems;

            var tagsToDelete = new List<Tag>();
            foreach (Tag item in selection)
            {
                var result = MessageBox.Show($"Wollen Sie das Tag '{item.Name}' wirklich löschen?", "Tag löschen", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    var targetTag = CurrentWiki.Tags.Find(t => t.Name == item.Name);
                    tagsToDelete.Add(targetTag);

                    foreach (var entry in CurrentWiki.Entries.Where(e => e.Tags.Find(t => t.Name == targetTag.Name) != null))
                    {
                        var targetTag2 = entry.Tags.Find(t => t.Name == targetTag.Name);
                        if (targetTag2 != null)
                        {
                            entry.Tags.Remove(targetTag2);
                            if (!entry.Tags.Any())
                            {
                                if (CurrentWiki.Tags.Find(t => t.Name == "Unbekannt") == null)
                                    CurrentWiki.Tags.Add(Models.Tag.Unknown);
                                entry.Tags.Add(Models.Tag.Unknown);
                            }
                        }
                    }
                }
            }

            foreach (var tag in tagsToDelete)
            {
                CurrentWiki.Tags.Remove(tag);
            }

            CurrentWiki.UpdateToC();
            Wiki.WriteWiki(WikiPath, CurrentWiki);
            CurrentWiki = Wiki.ReadWiki(WikiPath);
            RenderContent();
            Tag_Data.SelectedItems.Clear();
        }

        private void BTN_ReloadTemplate_Click(object sender, RoutedEventArgs e)
        {
            Wiki.WriteWiki(WikiPath, CurrentWiki, true);
            CurrentWiki = Wiki.ReadWiki(WikiPath);

            MessageBox.Show("Template neu geladen.");
        }

        private void BTN_AssignTag_Click(object sender, RoutedEventArgs e)
        {
            if (CB_AssignTag.SelectedItem == null)
                return;

            var targetTag = CurrentWiki.Tags.Find(t => t.Name == ((Tag)CB_AssignTag.SelectedValue).Name);
            if (targetTag != null && CurrentEntry != null && CurrentEntry.Tags.Find(t => t.Name == targetTag.Name) == null)
            {
                CurrentEntry.Tags.Add(targetTag);

                CurrentWiki.UpdateToC();
                Wiki.WriteWiki(WikiPath, CurrentWiki);
                CurrentWiki = Wiki.ReadWiki(WikiPath);
                RenderContent();
            }

            CB_AssignTag.SelectedItem = null;
        }

        private void BTN_DeleteTag_Click(object sender, RoutedEventArgs e)
        {
            var targetTag = CurrentEntry.Tags.Find(t => t.Name == ((Button)sender).ToolTip.ToString());
            if (targetTag != null)
            {
                var result = MessageBox.Show($"Wollen Sie das Tag '{targetTag.Name}' für den Eintrag '{CurrentEntry.Name}' wirklich löschen?", "Tag löschen", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    CurrentEntry.Tags.Remove(targetTag);
                    if (!CurrentEntry.Tags.Any())
                    {
                        if (CurrentWiki.Tags.Find(t => t.Name == "Unbekannt") == null)
                            CurrentWiki.Tags.Add(Models.Tag.Unknown);
                        CurrentEntry.Tags.Add(Models.Tag.Unknown);
                    }
                }
            }

            CurrentWiki.UpdateToC();
            Wiki.WriteWiki(WikiPath, CurrentWiki);
            CurrentWiki = Wiki.ReadWiki(WikiPath);
            RenderContent();
        }
    }
}
