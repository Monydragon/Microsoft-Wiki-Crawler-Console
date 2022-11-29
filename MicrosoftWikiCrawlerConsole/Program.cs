
using HtmlAgilityPack;
using System.Configuration;
using System.Text.RegularExpressions;

//Uses App.config for handling getting the variables for the 
var url = ConfigurationManager.AppSettings.Get("WebsiteAddress");
int numberOfResultsToReturn = 10;
int.TryParse(ConfigurationManager.AppSettings.Get("NumberOfResultsToReturn"), out numberOfResultsToReturn);
var wordsToExclude = ConfigurationManager.AppSettings.Get("WordsToExclude")?.Split(',').ToList();
List<ParsedWord> ParsedWords = new();

if (!string.IsNullOrEmpty(url))
{
    try
    {
        var response = await CallUrl(url);
        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(response);
        var htmlNode = doc.DocumentNode.Descendants("html").FirstOrDefault();
        string trimmedText = "";
        //This regex matches on all words and excludes special characters.
        string regexWordMatchPattern = @"(?<=\s+|^)[a-zA-Z]+(?=\s+|$)";
        if (htmlNode != null)
        {
            //This removes all the text before this string in the html innerText, So everything before the History section.
            trimmedText = htmlNode.InnerText.Substring(htmlNode.InnerText.IndexOf("Main article: History of Microsoft"));
            //This removes all the text after the string in the trimmedText the value here is the last value before the next section of the wiki article.
            trimmedText = trimmedText.Remove(trimmedText.IndexOf("&#91;155&#93;&#91;150&#93;"));

            //This foreach loop matches based on the pattern defined above. Checks the exluded words and skips to the next iteration if found a excluded word.
            //Then the loop tries to find if a parsedWord exists int he ParsedWords list and if it does it increments the count. if not it adds a new ParsedWord to the list. 
            foreach (Match word in Regex.Matches(trimmedText, regexWordMatchPattern, RegexOptions.IgnoreCase))
            {
                if (wordsToExclude?.Count > 0)
                {
                    if (wordsToExclude.Any(x => x == word.Value)) { continue; }
                }
                var parsedWord = ParsedWords.Find(x => x.Word == word.Value);
                if (parsedWord != null)
                {
                    parsedWord.Count++;
                }
                else
                {
                    ParsedWords.Add(new ParsedWord(word.Value, 1));
                }
            }
            //Finally if the ParsedWords count is greater than 0 it will order the words decending order by count and then take the top values based on the numberOfResultsToReturn
            //Writes out to the console the result and the count.
            if (ParsedWords.Count > 0)
            {
                var results = ParsedWords.OrderByDescending(x => x.Count).Take(numberOfResultsToReturn);
                Console.WriteLine("Words | # of occurrences");
                Console.WriteLine("________________________");
                foreach (var result in results)
                {
                    Console.WriteLine($"{result.Word} | {result.Count}");
                }
            }

        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Program ran into an exception: {ex.Message}");
    }

}

//This Async method starts a HttpClient response for the provided url and returns back the html as a string for that response.
static async Task<string> CallUrl(string fullUrl)
{
    string? response = "";
    try
    {
        HttpClient client = new HttpClient();
        response = await client.GetStringAsync(fullUrl);
        return response;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to get string data from url: {fullUrl}\n Message: {ex.Message}");
    }
    return response;
}

/// <summary>
/// This class is responsible for holding the parsed word and count.
/// </summary>
public class ParsedWord
{
    public string Word { get; set; }
    public int Count { get; set; }

    public ParsedWord(string word, int count)
    {
        Word = word;
        Count = count;
    }
}
