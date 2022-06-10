namespace TinyCart.Eric.Messages.V0;

public class Password
{
    [JsonProperty("data", Required = Required.Always)]
    public string Data { get; set; } = String.Empty;
    [JsonProperty("format", Required = Required.Always)]
    public string Format { get; set; } = String.Empty;

    public static string Decode(Password msg, RSAKeys keys)
    {
        switch (msg.Format)
        {
            case "plaintext":
                return msg.Data;
            case "rsa-base64":
                return TextUtil.UTF8NoBOM.GetString(keys.Decrypt(Convert.FromBase64String(msg.Data)));
            default:
                throw new InvalidOperationException($"unknown password format {msg.Format} provided to decode");
        }
    }

    public static Password Generate(string password, string format, RSAKeys keys)
    {
        switch (format)
        {
            case "plaintext":
                return new Password {
                    Data = password,
                    Format = format };
            case "rsa-base64":
                return new Password {
                    Data = Convert.ToBase64String(keys.Encrypt(TextUtil.UTF8NoBOM.GetBytes(password))),
                    Format = format };
            default:
                throw new InvalidOperationException($"unknown password format {format} provided to generate");
        }
    }
}