namespace Snip.LinkService.Services;

public class SlugService
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private readonly Random _random = Random.Shared;

    public string Generate(int length = 7)
    {
        return new string(Enumerable
            .Range(0, length)
            .Select(_ => Alphabet[_random.Next(Alphabet.Length)])
            .ToArray());
    }
}