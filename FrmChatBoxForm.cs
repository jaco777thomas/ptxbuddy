using System;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using Paratext.PluginInterfaces;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Drawing.Imaging;


namespace Ptxbuddy
{
    public partial class FrmChatBoxForm : EmbeddedPluginControl
    {
        #region Member variables
        private IVerseRef m_reference;
        private IProject m_project;
        private Regex m_regexWordExtractor;
        private Thread m_updateThread;
        private IWindowPluginHost m_host;
        private string m_selectedText;
        #endregion

        #region Constructor
        private Panel typingIndicatorPanel;
        private Label lblTyping;
        private System.Windows.Forms.Timer typingTimer;
        private int dotCount = 0;

        private string currentImageBase64 = null;

        private static readonly Dictionary<string, string> BookNames = new Dictionary<string, string>
        {
            { "GEN", "Genesis" },
            { "EXO", "Exodus" },
            { "LEV", "Leviticus" },
            { "NUM", "Numbers" },
            { "DEU", "Deuteronomy" },
            { "JOS", "Joshua" },
            { "JDG", "Judges" },
            { "RUT", "Ruth" },
            { "1SA", "1 Samuel" },
            { "2SA", "2 Samuel" },
            { "1KI", "1 Kings" },
            { "2KI", "2 Kings" },
            { "1CH", "1 Chronicles" },
            { "2CH", "2 Chronicles" },
            { "EZR", "Ezra" },
            { "NEH", "Nehemiah" },
            { "EST", "Esther" },
            { "JOB", "Job" },
            { "PSA", "Psalms" },
            { "PRO", "Proverbs" },
            { "ECC", "Ecclesiastes" },
            { "SNG", "Song of Solomon" },
            { "ISA", "Isaiah" },
            { "JER", "Jeremiah" },
            { "LAM", "Lamentations" },
            { "EZK", "Ezekiel" },
            { "DAN", "Daniel" },
            { "HOS", "Hosea" },
            { "JOL", "Joel" },
            { "AMO", "Amos" },
            { "OBA", "Obadiah" },
            { "JON", "Jonah" },
            { "MIC", "Micah" },
            { "NAM", "Nahum" },
            { "HAB", "Habakkuk" },
            { "ZEP", "Zephaniah" },
            { "HAG", "Haggai" },
            { "ZEC", "Zechariah" },
            { "MAL", "Malachi" },
            { "MAT", "Matthew" },
            { "MRK", "Mark" },
            { "LUK", "Luke" },
            { "JHN", "John" },
            { "ACT", "Acts" },
            { "ROM", "Romans" },
            { "1CO", "1 Corinthians" },
            { "2CO", "2 Corinthians" },
            { "GAL", "Galatians" },
            { "EPH", "Ephesians" },
            { "PHP", "Philippians" },
            { "COL", "Colossians" },
            { "1TH", "1 Thessalonians" },
            { "2TH", "2 Thessalonians" },
            { "1TI", "1 Timothy" },
            { "2TI", "2 Timothy" },
            { "TIT", "Titus" },
            { "PHM", "Philemon" },
            { "HEB", "Hebrews" },
            { "JAS", "James" },
            { "1PE", "1 Peter" },
            { "2PE", "2 Peter" },
            { "1JN", "1 John" },
            { "2JN", "2 John" },
            { "3JN", "3 John" },
            { "JUD", "Jude" },
            { "REV", "Revelation" }
        };
        public string UserAction = "";
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        // Define RECT structure for GetWindowRect
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        #endregion

        #region Implementation of EmbeddedPluginControl
        public override void OnAddedToParent(IPluginChildWindow parent, IWindowPluginHost host, string state)
        {
            m_host = host;
            parent.SetTitle(Ptxbuddy.pluginName);

            //parent.VerseRefChanged += Parent_VerseRefChanged;
            //parent.ProjectChanged += Parent_ProjectChanged;

            SetProject(parent.CurrentState.Project);
            m_reference = parent.CurrentState.VerseRef;
            m_selectedText = state;
            //selectedTextToolStripMenuItem.Checked = state != null;
        }

        private void SetProject(IProject project)
        {
            if (m_project != null)
                m_project.ProjectDeleted -= HandleProjectDeleted;

            m_project = project;
            project.ProjectDeleted += HandleProjectDeleted;
            m_regexWordExtractor = new Regex(m_project.Language.WordMatchRegex, RegexOptions.Compiled);
        }

        private void HandleProjectDeleted()
        {
            m_project = null;
        }

        public override string GetState()
        {
            return m_selectedText;
        }

        public override void DoLoad(IProgressInfo progress)
        {
            //UpdateWordle(new ProgressInfoWrapper(progress));
        }

        #endregion

        #region Overridden Form methods
        protected override void OnHandleDestroyed(EventArgs e)
        {
            //m_host.ActiveWindowSelectionChanged -= ActiveWindowSelectionChanged;
            base.OnHandleDestroyed(e);
        }
        #endregion

       
        private void PositionNextToActiveWindow()
        {
            // Get the handle of the active window
            IntPtr activeWindowHandle = GetForegroundWindow();

            if (activeWindowHandle != IntPtr.Zero)
            {
                // Get the position and size of the active window
                RECT activeWindowRect;
                if (GetWindowRect(activeWindowHandle, out activeWindowRect))
                {
                    // Calculate the position for your form (e.g., to the right of the active window)
                    int newX = activeWindowRect.Right; // Place to the right of the active window
                    int newY = activeWindowRect.Top;   // Align with the top of the active window

                    Rectangle screenBounds = Screen.GetWorkingArea(this); // Get the working area of the screen
                    if (newX + this.Width > screenBounds.Right)
                    {
                        newX = screenBounds.Right - this.Width; // Adjust to fit within the screen
                    }
                    if (newY + this.Height > screenBounds.Bottom)
                    {
                        newY = screenBounds.Bottom - this.Height; // Adjust to fit within the screen
                    }
                    if (newX < screenBounds.Left)
                    {
                        newX = screenBounds.Left; // Ensure the form doesn't go off the left edge
                    }
                    if (newY < screenBounds.Top)
                    {
                        newY = screenBounds.Top; // Ensure the form doesn't go off the top edge
                    }

                    // Set the location of your form
                    //this.StartPosition = FormStartPosition.Manual;
                    this.Location = new Point(newX, newY);
                }
            }
            else
            {
                // Default position if no active window is found
               // this.StartPosition = FormStartPosition.WindowsDefaultLocation;
            }
        }

        
        public FrmChatBoxForm()
        {
            InitializeComponent();
            listBox1.Visible = false;
            listBox1.DrawMode = DrawMode.OwnerDrawFixed;
            listBox1.DrawItem += listBox1_DrawItem;
             btnagent.Text = "Select an Agent";
                  
          
            Load_Projects();
            UserAction = "";
            txtPrompt.Focus();
            InitializeTypingIndicator();
            InitializeTimer();
            Resize_Controls();

        }
        private void Resize_Controls()
        {
            panel1.Anchor =  AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            chatArea.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            chatArea.SizeChanged += ChatArea_SizeChanged;
            txtPrompt.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBoxResult.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btnAiCommunicator.Dock = DockStyle.None;
            btnAiCommunicator.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnAiCommunicator.MaximumSize = new Size(100, 50);
            btnAiCommunicator.MinimumSize = new Size(55, 45);
            btnAiCommunicator.Location = new Point(
            panel1.Width - btnAiCommunicator.Width - 5,
            panel1.Height - btnAiCommunicator.Height - 5
            );
            btnscrnsht.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnscrnsht.MaximumSize = new Size(100, 50);
            btnscrnsht.MinimumSize = new Size(55, 45);
            btnagent.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnagent.MaximumSize = new Size(140, 50);
            btnagent.MinimumSize = new Size(140, 25);
            btnagent.Left = btnAiCommunicator.Left - btnagent.Width - 50; 
           // btnagent.Right = btnAiCommunicator.Left;
            button1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            button1.MaximumSize = new Size(100,50);
            button1.MinimumSize = new Size(55, 45);
            button2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            button2.MaximumSize = new Size(100, 50);
            button2.MinimumSize = new Size(55, 45);
            button3.Anchor = AnchorStyles.Bottom;
            button3.MaximumSize = new Size(100, 50);
            button3.MinimumSize = new Size(55, 45);
            button3.Left = (panel1.Width - button3.Width) / 2;
            button3.Top = panel1.Height - button3.Height - 5;
            button5.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            button5.MaximumSize = new Size(55, 45);
            button5.MinimumSize = new Size(55, 45);
            listBox1.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            listBox1.MaximumSize = new Size(btnagent.Width, 150);
            listBox1.Top = btnagent.Top - listBox1.Height - 2; 
            listBox1.Left = btnagent.Left;
            listBox1.BringToFront();

        }
       

        private void InitializeTypingIndicator()
        {
            typingIndicatorPanel = new RoundedPanel(); 
            //typingIndicatorPanel.CornerRadius = 15;
            typingIndicatorPanel.BackColor = Color.LightGray;
            typingIndicatorPanel.Size = new Size(120, 30);
            typingIndicatorPanel.Visible = false;

            lblTyping = new Label();
            lblTyping.Text = "AI is typing";
            lblTyping.Location = new Point(10, 5);
            lblTyping.AutoSize = true;
            typingIndicatorPanel.Controls.Add(lblTyping);

            // Add to chat area
            chatArea.Controls.Add(typingIndicatorPanel);
        }
        
        private void InitializeTimer()
        {
            typingTimer = new System.Windows.Forms.Timer
            {
                Interval = 500,
                Enabled = false
            };
            typingTimer.Tick += TypingTimer_Tick;
        }
        private void TypingTimer_Tick(object sender, EventArgs e)
        {
            dotCount = (dotCount + 1) % 4;
            string dots = new string('.', dotCount);
            lblTyping.Text = $"AI is typing{dots}";

           
            typingIndicatorPanel.Location = new Point(
                10,
                chatArea.VerticalScroll.Value + chatArea.Height - typingIndicatorPanel.Height - 10
            );
        }
        private void AddChatBubble(string sender, string message, Color bubbleColor)
        {
           
            RoundedPanel chatBubble = new RoundedPanel();
            chatBubble.BackColor = bubbleColor;
             chatBubble.CornerRadius = 20;
            chatBubble.AutoSize = false; 
            chatBubble.Anchor = AnchorStyles.Left | AnchorStyles.Right; 
            chatBubble.Width = chatArea.ClientSize.Width - 5; 
            chatBubble.Padding = new Padding(10);


          
            RichTextBox messageLabel = new RichTextBox();
            messageLabel.Text = message;
            messageLabel.BackColor = chatBubble.BackColor;
            messageLabel.BorderStyle = BorderStyle.None;
            messageLabel.Font = new Font("Segoe UI", 10);
            messageLabel.AutoSize = false;
            messageLabel.WordWrap = true;
            messageLabel.ScrollBars = RichTextBoxScrollBars.None;

            
            int textWidth = chatBubble.Width - chatBubble.Padding.Horizontal;
            int textHeight = CalculateTextHeight(message, messageLabel.Font, textWidth);
            messageLabel.Size = new Size(textWidth, textHeight);

            chatBubble.Controls.Add(messageLabel);


           int yPosition = 10; 
            if (chatArea.Controls.Count > 0)
            {
                  yPosition = chatArea.Controls[chatArea.Controls.Count - 1].Bottom + 10;
            }
            chatBubble.Location = new Point(0, yPosition);

            System.Windows.Forms.Button copyButton = new System.Windows.Forms.Button();
            copyButton.Text = "Copy";
            copyButton.FlatStyle = FlatStyle.Standard;
            copyButton.Font = new Font("Segoe UI", 7);
            copyButton.BackColor = chatBubble.BackColor;
            copyButton.Size = new Size(45, 20);
            copyButton.Location = new Point(chatBubble.Width - copyButton.Width - 5, 5);
            copyButton.Click += (s, e) => CopyMessageToClipboard(message);
            chatBubble.Controls.Add(copyButton);
            copyButton.BringToFront();

           
            chatBubble.Resize += (s, e) =>
            {
                messageLabel.Width = chatBubble.Width - chatBubble.Padding.Horizontal;
                textHeight = CalculateTextHeight(message, messageLabel.Font, messageLabel.Width);
                messageLabel.Height = textHeight;
                chatBubble.Height = textHeight + chatBubble.Padding.Vertical;

                copyButton.Location = new Point(
            chatBubble.Width - copyButton.Width - 5, 5
            );
            };

           
            chatArea.Controls.Add(chatBubble);
         
            chatArea.ScrollControlIntoView(chatBubble);

           
        }
        private void ChatArea_SizeChanged(object sender, EventArgs e)
        {
            
            int yPosition = 10;
            foreach (Control control in chatArea.Controls)
            {
                if (control is RoundedPanel chatBubble)
                {
                     chatBubble.Width = chatArea.ClientSize.Width - 5;

                    chatBubble.Location = new Point(0, yPosition);
                    yPosition = chatBubble.Bottom + 10;
                }
            }
        }
        private int CalculateTextHeight(string text, Font font, int maxWidth)
        {
            using (Graphics g = CreateGraphics())
            {
                SizeF size = g.MeasureString(text, font, maxWidth);
                return (int)Math.Ceiling(size.Height);
            }
        }
        private void CopyMessageToClipboard(string message)
        {
            Thread thread = new Thread(() => Clipboard.SetDataObject(message, true));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            MessageBox.Show("Message copied to clipboard!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void Load_Projects()
        {
            string paratextProjectsPath = @"C:\My Paratext 9 Projects";

            if (Directory.Exists(paratextProjectsPath))
            {
                Console.WriteLine("Valid Paratext Projects:");
                string[] projectFolders = Directory.GetDirectories(paratextProjectsPath);
                cmbProject.Items.Clear();
                foreach (string folder in projectFolders)
                {
                   
                    bool hasUsfmFile = Directory.GetFiles(folder, "*.SFM").Length > 0;
                    bool hasSettingsFile = File.Exists(Path.Combine(folder, "Settings.xml"));

                    if (hasUsfmFile && hasSettingsFile)
                    {
                        string projectName = Path.GetFileName(folder);
                        cmbProject.Items.Add(projectName);
                    }
                }
            }
            else
            {
                Console.WriteLine("Paratext projects directory not found.");
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {


                textBoxResult.Text = string.Empty;
                string bookCode = cmbBook.SelectedValue?.ToString();
                string chapter = cmbChapter.SelectedItem?.ToString();
                string ProjectName = cmbProject.SelectedItem?.ToString();

                if (bookCode == null || chapter == null || ProjectName == null)
                {
                    MessageBox.Show("Please give inputs", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                string projectPath = @"C:\My Paratext 9 Projects\" + ProjectName + "\\";

                string filePath = projectPath + bookCode + ".SFM";  
                string sfmContent = File.ReadAllText(filePath);

                // string input = "copy MAT 2:2-5"; 
                int VerseFrom = int.Parse(cmbVerseFrm.SelectedItem.ToString());
                int VerseTo = int.Parse(cmbVerseTo.SelectedItem.ToString());
                string extractedText = ExtractVerses(sfmContent, chapter, VerseFrom, VerseTo);
              //  textBoxResult.Text = extractedText;
               // messageLabel.Text = extractedText;
                AddChatBubble("Me", extractedText, Color.LightBlue);
            }
            catch { }   

        }

        private void cmbProject_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                LoadBooks();
            }
            catch 
            {
            
            }
        }
        static string ExtractVerses(string text, string startChapter, int startVerse, int endVerse)
        {
            //  Chapter 
            string selectedChapter = startChapter.ToString();
            string chapterPattern = @"\\c " + selectedChapter + "\\b";
            Match chapterMatch = Regex.Match(text, chapterPattern);

            if (!chapterMatch.Success) return "Chapter not found.";

            int chapterIndex = chapterMatch.Index;

          
            int nextChapterIndex = Regex.Match(text.Substring(chapterIndex + 3), @"\\c \d+").Success
                ? text.IndexOf("\\c", chapterIndex + 3)
                : text.Length;

            string chapterText = text.Substring(chapterIndex, nextChapterIndex - chapterIndex);

           
            string versePattern = @"\\v (\d+) (.*?)((?=\\v \d+)|(?=\\c \d+)|$)";
            MatchCollection verses = Regex.Matches(chapterText, versePattern, RegexOptions.Singleline);

            string result = "Chapter " + selectedChapter + @":\n";
            bool foundVerses = false;

            foreach (Match verse in verses)
            {
                int verseNumber = int.Parse(verse.Groups[1].Value);
                if (verseNumber >= startVerse && verseNumber <= endVerse)
                {
                    result += $"\\v {verse.Groups[1].Value} {verse.Groups[2].Value.Trim()}\n";
                    foundVerses = true;
                }
            }

            return foundVerses ? result.Trim() : "No verses found in this Chapter .";
        }

      
        private void LoadBooks()
        {
            try
            {
                string ProjectName = cmbProject.SelectedItem.ToString();
                string projectPath = @"C:\My Paratext 9 Projects\" + ProjectName + "\\";
                string bookName = string.Empty;
                string output = string.Empty;
                cmbBook.DataSource = null;
                if (Directory.Exists(projectPath))
                {
                    
                    List<Book> bookData = new List<Book>();

                    var usfmFiles = Directory.GetFiles(projectPath, "*.SFM");

                    var bookCodes = usfmFiles
                        .Select(file => Path.GetFileNameWithoutExtension(file))
                        .OrderBy(book => book);

                    foreach (string bookCode in bookCodes)
                    {

                        output = bookCode.Substring(2, 3);
                        if (BookNames.TryGetValue(output, out bookName))
                        {
                            bookData.Add(new Book { DisplayName = bookName, Code = bookCode });
                        }
                        else
                        {
                            bookData.Add(new Book { DisplayName = bookCode, Code = bookCode });
                        }
                    }
                    cmbBook.DataSource = bookData;
                    cmbBook.DisplayMember = "DisplayName";
                    cmbBook.ValueMember = "Code";

                    if (cmbBook.Items.Count > 0)
                    {
                        cmbBook.SelectedIndex = 0;
                    }
                }
                else
                {
                    MessageBox.Show("Project directory not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch
            {

            }
        }
        private void LoadChapters()
        {

            try { 
            string ProjectName = cmbProject.SelectedItem.ToString();
            string projectPath = @"C:\My Paratext 9 Projects\" + ProjectName + "\\";

            string bookCode = cmbBook.SelectedValue?.ToString();

            //string code = GetBookCode(BookNames, bookCode);
            if (bookCode == "ChatWithAIPlugin.Book" || bookCode == null)
            {
                return;
            }

            string usfmFilePath = Path.Combine(projectPath, bookCode + ".SFM");

            if (File.Exists(usfmFilePath))
            {
                // Read the USFM file
                string[] lines = File.ReadAllLines(usfmFilePath);

                // Extract chapter numbers
                var chapters = lines
                    .Where(line => line.StartsWith("\\c ")) // Find lines with chapter markers
                    .Select(line => int.Parse(line.Substring(3).Trim())) // Extract chapter numbers
                    .Distinct() // Remove duplicates
                    .OrderBy(chapter => chapter); // Sort chapters
                cmbChapter.Items.Clear();
                // Add chapters to the ComboBox
                foreach (int chapter in chapters)
                {
                    cmbChapter.Items.Add(chapter);
                }

                // Select the first chapter by default
                if (cmbChapter.Items.Count > 0)
                {
                    cmbChapter.SelectedIndex = 0;
                }
            }
            else
            {
                MessageBox.Show("USFM file not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            }
            catch
            {

            }
        }

        private void cmbBook_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadChapters();
        }

        private void cmbChapter_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadVerses();
        }
        private int CountVersesInChapter(string text, int chapterNumber)
        {
            // Locate the chapter (\c X)
            string chapterPattern = $@"\\c {chapterNumber}\b";
            Match chapterMatch = Regex.Match(text, chapterPattern);

            if (!chapterMatch.Success) return 0; // Chapter not found

            int chapterIndex = chapterMatch.Index;

            // Find where the next chapter starts
            int nextChapterIndex = Regex.Match(text.Substring(chapterIndex + 3), @"\\c \d+").Success
                ? text.IndexOf("\\c", chapterIndex + 3)
                : text.Length;

            // Extract the text of the chapter
            string chapterText = text.Substring(chapterIndex, nextChapterIndex - chapterIndex);

            // Count verse markers (\v X)
            MatchCollection verseMatches = Regex.Matches(chapterText, @"\\v (\d+)");

            return verseMatches.Count; // Return the number of verses
        }
        private void LoadVerses()
        {
            string ProjectName = cmbProject.SelectedItem.ToString();
            string projectPath = @"C:\My Paratext 9 Projects\" + ProjectName + "\\";
            string bookCode = cmbBook.SelectedValue?.ToString();
            string filePath = projectPath + bookCode + ".SFM";

            if (!File.Exists(filePath))
            {
                MessageBox.Show("SFM file not found.");
                return;
            }

            if (!int.TryParse(cmbChapter.SelectedItem.ToString(), out int chapterNumber))
            {
                MessageBox.Show("Please enter a valid chapter number.");
                return;
            }

            string sfmContent = File.ReadAllText(filePath);
            int verseCount = CountVersesInChapter(sfmContent, chapterNumber);

            if (verseCount == 0)
            {
                MessageBox.Show($"No verses found in Chapter {chapterNumber}.");
                return;
            }

          
            cmbVerseFrm.Items.Clear();
            cmbVerseTo.Items.Clear();
            for (int i = 1; i <= verseCount; i++)
            {
                cmbVerseFrm.Items.Add(i);
                cmbVerseTo.Items.Add(i);
                //cmbVerseTo.Items.Add($"Verse {i}");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (textBoxResult.Text != null)
                {
                    string bookCode = cmbBook.SelectedValue?.ToString();
                    string chapter = cmbChapter.SelectedItem?.ToString();
                    string ProjectName = cmbProject.SelectedItem?.ToString();

                    if (bookCode == null || chapter == null || ProjectName == null)
                    {
                        MessageBox.Show("Please give inputs", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    string projectPath = @"C:\My Paratext 9 Projects\" + ProjectName + "\\";

                    string filePath = projectPath + bookCode + ".SFM";  
                    string sfmContent = File.ReadAllText(filePath);

                    int VerseFrom = int.Parse(cmbVerseFrm.SelectedItem.ToString());
                    int VerseTo = int.Parse(cmbVerseTo.SelectedItem.ToString());

                    // Extract verses 
                    string extractedVerses = textBoxResult.Text; 

                    if (extractedVerses.StartsWith("Chapter"))
                    {
                        
                        sfmContent = ReplaceVerses(sfmContent, chapter, VerseFrom, VerseTo, extractedVerses);

                        File.WriteAllText(filePath, sfmContent);

                        MessageBox.Show("Verses replaced successfully..", "Success", MessageBoxButtons.OK, MessageBoxIcon.Error);
                       // Console.WriteLine("Verses replaced successfully.");
                    }
                    else
                    {
                        Console.WriteLine(extractedVerses); 
                    }
                }
            }
            catch
            { }

         
        }
        static string ReplaceVerses(string text, string chapter, int startVerse, int endVerse, string newVerses)
        {
           
            string chapterPattern = @"\\c " + chapter + "\\b";
            Match chapterMatch = Regex.Match(text, chapterPattern);

            if (!chapterMatch.Success) return text; //

            int chapterIndex = chapterMatch.Index;

            
            int nextChapterIndex = Regex.Match(text.Substring(chapterIndex + 3), @"\\c \d+").Success
                ? text.IndexOf("\\c", chapterIndex + 3)
                : text.Length;

            string chapterText = text.Substring(chapterIndex, nextChapterIndex - chapterIndex);

           
            string versePattern = @"\\v (\d+) (.*?)((?=\\v \d+)|(?=\\c \d+)|$)";
            MatchCollection verses = Regex.Matches(chapterText, versePattern, RegexOptions.Singleline);

            int startIndex = -1;
            int endIndex = -1;

            foreach (Match verse in verses)
            {
                int verseNumber = int.Parse(verse.Groups[1].Value);
                if (verseNumber == startVerse && startIndex == -1)
                {
                    startIndex = verse.Index + chapterIndex;
                }
                if (verseNumber == endVerse)
                {
                    endIndex = verse.Index + verse.Length + chapterIndex;
                }
            }

            if (startIndex == -1 || endIndex == -1) return text; // If verses not found, return original text

           
            string updatedText = text.Substring(0, startIndex) + newVerses + text.Substring(endIndex);

            return updatedText;
        }

      
        private static readonly HttpClient client = new HttpClient();


        private void AppendUserMessage(string message)
        {
            if (textBoxResult.InvokeRequired)
            {
                textBoxResult.Invoke((Action)(() => AppendUserMessage(message)));
            }
            else
            {
                
                textBoxResult.AppendText("\n\nMe: ");
                               
                int start = textBoxResult.TextLength;

                textBoxResult.AppendText(message);

                int end = textBoxResult.TextLength;

                textBoxResult.Select(start, end - start);
                textBoxResult.SelectionBackColor = Color.LightBlue; 
                textBoxResult.SelectionColor = Color.Black; 

                textBoxResult.Select(end, 0);

                textBoxResult.ScrollToCaret();
            }
        }

        private void AppendBotMessage(string message)
        {
            if (textBoxResult.InvokeRequired)
            {
                textBoxResult.Invoke((Action)(() => AppendBotMessage(message)));
            }
            else
            {
                textBoxResult.AppendText("\n\nAI: ");

                int start = textBoxResult.TextLength;

                textBoxResult.AppendText(message);

                int end = textBoxResult.TextLength;

                textBoxResult.Select(start, end - start);
                textBoxResult.SelectionBackColor = Color.LightGray;
                textBoxResult.SelectionColor = Color.Black;

                textBoxResult.Select(end, 0);

                textBoxResult.ScrollToCaret();
            }
        }

        private async void btnAiCommunicator_Click(object sender, EventArgs e)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            string userMessage = txtPrompt.Text;

            if (!string.IsNullOrEmpty(userMessage))
            {
                  //AppendUserMessage(userMessage);
                AddChatBubble("Me", userMessage, Color.LightBlue);

                typingIndicatorPanel.Visible = true;
                typingTimer.Start();
                UpdateTypingIndicatorPosition();

                string webhookUrl = "https://ptxbuddy.app.n8n.cloud/webhook/ptxbuddy"; 
               // string webhookUrl = "https://jacob777thomas.app.n8n.cloud/webhook/d75282e6-cc2e-47af-9a78-404f152c912a";

                string apiKey = "pbuddy4P9.4"; 
                string headerName = "ptxbuddy";

                string queryText = txtPrompt.Text;  
                string sys_prompt = messageLabel.Text;
                if (UserAction == "")
                {
                    UserAction = "General Traslation";
                }
                var requestData = new
                {
                    action = UserAction,
                    query = queryText,
                    txt = queryText,
                    in_language = "Malayalam",
                    out_language = "English"
                   
                };

                string jsonContent = JsonConvert.SerializeObject(requestData);

                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add(headerName, apiKey);

                        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                        HttpResponseMessage response = await client.PostAsync(webhookUrl, content);
                        string result = await response.Content.ReadAsStringAsync();

                        //string formattedResult = result.Split(new[] { "\"output\":\"" }, StringSplitOptions.None)[1]
                        //                 .TrimEnd('"', '}');

                        //string formattedText = formattedResult.Replace("\\\\n", "\n") 
                        //                   .Replace("\\n", "\n")
                        //                   .Replace("\\v ", "\n**Verse ")
                        //                   .Replace("Chapter", "**Chapter");

                        //if (formattedText.EndsWith("\"}]"))
                        //{
                        //    formattedText = formattedText.Substring(0, formattedText.Length - 3);
                        //}

                        //Console.WriteLine($"Response: {formattedText}");
                        string outputValue = JsonParser.ExtractOutputValue(result);
                        AddChatBubble($"\nAI:", outputValue, Color.LightGray);
                        //  AppendBotMessage($"Response: {formattedText}");
                        //Invoke((Action)(() => textBoxResult.AppendText($"\nAI: {formattedText}\n")));
                        txtPrompt.Clear();
                        currentImageBase64 = null;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Exception: {ex.Message}");
                }
                finally
                {
                    typingIndicatorPanel.Visible = false;
                    typingTimer.Stop();
                    dotCount = 0;
                    lblTyping.Text = "AI is typing";
                }
            }
        }
        private void chatArea_Scroll(object sender, ScrollEventArgs e)
        {
            UpdateTypingIndicatorPosition();
        }
        private void UpdateTypingIndicatorPosition()
        {
            if (typingIndicatorPanel.Visible)
            {
                typingIndicatorPanel.BringToFront();
                typingIndicatorPanel.Location = new Point(
                    10,
                    chatArea.VerticalScroll.Value + chatArea.Height - typingIndicatorPanel.Height - 10
                );
            }
        }

        private void FrmTestChat_Load(object sender, EventArgs e)
        {
            txtPrompt.Focus();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            listBox1.Visible = !listBox1.Visible;
        }

        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            e.DrawBackground();

            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                e.Graphics.FillRectangle(Brushes.LightBlue, e.Bounds);
                e.Graphics.DrawString(listBox1.Items[e.Index].ToString(), e.Font, Brushes.Black, e.Bounds);
            }
            else
            {
                e.Graphics.FillRectangle(Brushes.LightSlateGray, e.Bounds);
                e.Graphics.DrawString(listBox1.Items[e.Index].ToString(), e.Font, Brushes.White, e.Bounds);
            }

            e.DrawFocusRectangle();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnagent.Text = "Select an Agent";
            if (listBox1.SelectedItem != null)
            {
                string selectedItem = listBox1.SelectedItem.ToString();
                UserAction = selectedItem;
                btnagent.Text = selectedItem;
                listBox1.Visible = false;
              // MessageBox.Show("You selected: " + selectedItem);
            }
        }
        //private void StartAnimation(Control control)
        //{
        //    Timer animationTimer = new Timer();
        //    int targetY = chatArea.Controls.Count > 1 ? chatArea.Controls[chatArea.Controls.Count - 2].Bottom + 10 : 10;
        //    int step = 5;

        //    animationTimer.Interval = 20;
        //    animationTimer.Tick += (s, e) =>
        //    {
        //        if (control.Location.Y < targetY)
        //        {
        //            control.Location = new Point(control.Location.X, control.Location.Y + step);
        //            chatArea.ScrollControlIntoView(control);
        //        }
        //        else
        //        {
        //            animationTimer.Stop();
        //            chatArea.ScrollControlIntoView(control); 
        //        }
        //    };
        //    animationTimer.Start();
        //}

        private void button5_Click_1(object sender, EventArgs e)
        {
            chatArea.Controls.Clear();
        }

        private void btnscrnsht_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp";
                openFileDialog.Title = "Select an Image File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string filePath = openFileDialog.FileName;

                        const int MAX_SIZE_MB = 5;
                        FileInfo fileInfo = new FileInfo(filePath);
                        if (fileInfo.Length > MAX_SIZE_MB * 1024 * 1024)
                        {
                            MessageBox.Show($"Maximum file size is {MAX_SIZE_MB}MB");
                            return;
                        }

                        using (System.Drawing.Image originalImage = System.Drawing.Image.FromFile(filePath))
                        {
                         
                            Bitmap imageCopy = new Bitmap(originalImage);

                            currentImageBase64 = ImageToBase64(imageCopy);

                            AddImageBubble(imageCopy);
                        }
                    }
                    catch (OutOfMemoryException)
                    {
                        MessageBox.Show("Invalid image file format");
                    }
                    catch (FileNotFoundException)
                    {
                        MessageBox.Show("File not found");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading image: {ex.Message}");
                    }
                }
            }
        }
        private string ImageToBase64(System.Drawing.Image image)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    image.Save(ms, ImageFormat.Png);
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Image conversion failed: {ex.Message}");
                return null;
            }
        }
        private void AddImageBubble(System.Drawing.Image image)
        {
            try
            {
                RoundedPanel imageBubble = new RoundedPanel();
                imageBubble.BackColor = Color.LightBlue;
                imageBubble.CornerRadius = 20;
                imageBubble.Size = new Size(200, 200);
                imageBubble.Padding = new Padding(5);

                PictureBox pb = new PictureBox();
                pb.SizeMode = PictureBoxSizeMode.Zoom;

                pb.Image = new Bitmap(image);

                pb.Size = new Size(190, 190);

                if (image.Width > 800 || image.Height > 600)
                {
                    double ratio = Math.Min(800.0 / image.Width, 600.0 / image.Height);
                    pb.Size = new Size((int)(image.Width * ratio), (int)(image.Height * ratio));
                }

                ContextMenuStrip menu = new ContextMenuStrip();
                menu.Items.Add("Remove", null, (s, args) =>
                {
                    chatArea.Controls.Remove(imageBubble);
                    currentImageBase64 = null;
                    pb.Image?.Dispose();
                    imageBubble.Dispose();
                });
                pb.ContextMenuStrip = menu;

                imageBubble.Controls.Add(pb);

                int yPosition = chatArea.Controls.Count > 0
                    ? chatArea.Controls[chatArea.Controls.Count - 1].Bottom + 10
                    : 10;

                imageBubble.Location = new Point(10, yPosition);
                chatArea.Controls.Add(imageBubble);
                chatArea.ScrollControlIntoView(imageBubble);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating image bubble: {ex.Message}");
            }
        }
    }

    public class RoundedPanel : Panel
    {
        public int CornerRadius { get; set; } = 20;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using (GraphicsPath path = new GraphicsPath())
            {
                //path.AddArc(0, 0, CornerRadius, CornerRadius, 180, 90);
                //path.AddArc(Width - CornerRadius, 0, CornerRadius, CornerRadius, 270, 90);
                //path.AddArc(Width - CornerRadius, Height - CornerRadius, CornerRadius, CornerRadius, 0, 90);
                //path.AddArc(0, Height - CornerRadius, CornerRadius, CornerRadius, 90, 90);
                //path.CloseFigure();

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(new SolidBrush(BackColor), path);
            }
        }
    }

}

