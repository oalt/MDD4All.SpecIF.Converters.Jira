namespace MDD4All.SpecIF.Converters.Jira
{
    public class SpecIfToJiraConverter
    {

        public string ConvertDescription(string xhtml)
        {
            var converter = new ReverseMarkdown.Converter();

            string result = converter.Convert(xhtml);

            return result;
        }

    }
}
