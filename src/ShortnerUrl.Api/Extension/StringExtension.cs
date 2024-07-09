namespace ShortenerUrl.Api.Extension;

public class StringExtension
{
    private readonly Random _random;
    private readonly int _maxLength;
    private readonly string _acceptedChars;
    public StringExtension()
    {
        _random = new();
        _acceptedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        _maxLength = 7;
    }

    public string BuildUniqueCode()
    {
        var codeChars = new char[_maxLength];

        int maxValue = _acceptedChars.Length;

        for (var i = 0; i < _maxLength; i++)
        {
            var randomIndex = _random.Next(maxValue);

            codeChars[i] = _acceptedChars[randomIndex];
        }

        var code = new string(codeChars);

        return code;
    }

           
}
