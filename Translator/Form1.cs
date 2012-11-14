
namespace Translator
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Windows.Forms;
    using YABFcompiler;
    using YABFcompiler.LanguageParsers;

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            PopulateLanguageList(SourceInputLanguage, SourceOutputLanguage);

            SourceInputLanguage.SelectedIndex = 0;
            SourceOutputLanguage.SelectedIndex = 1;
        }

        private void TranslateButton_Click(object sender, EventArgs e)
        {
            Parser inputParser = GetParser(SourceInputLanguage.SelectedItem.ToString()),
                outputParser = GetParser(SourceOutputLanguage.SelectedItem.ToString());

            SourceOutput.Text = Translate(inputParser, outputParser, SourceInput.Text);
        }

        private Parser GetParser(string language)
        {
            Parser parser = null;

            switch (language) 
                {
                    case "Brainfuck": parser = new BrainfuckParser(); break;
                    case "Ook!": parser = new OokParser(); break;
                    case "ShortOok!": parser = new ShortOokParser(); break;
                }

            return parser;
        }

        private static string Translate(Parser inputParser, Parser outputParser, string sourceInput)
        {
            var inputInstructions = inputParser.GenerateDIL(sourceInput);
            var sb = new StringBuilder();
            foreach (var languageInstruction in inputInstructions.ToArray())
            {
                sb.Append(outputParser.AllowedInstructions.GetBySecond(languageInstruction));
            }

            return sb.ToString();
        }

        private static void PopulateLanguageList(params ComboBox[] boxes)
        {
            foreach (var box in boxes)
            {
                box.Items.AddRange(new object[] { "Brainfuck", "Ook!", "ShortOok!" });
            }
        }
    }
}
 