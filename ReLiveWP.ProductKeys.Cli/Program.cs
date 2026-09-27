using Org.BouncyCastle.Security;
using ReLiveWP.ProductKeys;
using ReLiveWP.ProductKeys.Cli;

var arguments = new CommandLineArguments(args);
var keyFilePath = arguments.GetOption("--key-file") ?? SigningKeyFile.DefaultPath;
var iniSectionName = arguments.GetOption("--section") ?? SigningKeyFile.DefaultIniSectionName;

try
{
    return arguments.Command switch
    {
        "new-curve" => CreateCurve(keyFilePath, iniSectionName, arguments.HasFlag("--force")),
        "print-ini" => PrintIniSection(keyFilePath, iniSectionName),
        "generate" => GenerateProductKeys(keyFilePath, arguments),
        "verify" => VerifyProductKey(keyFilePath, arguments.RequirePositional(0, "product key")),
        "decode" => DecodeProductKey(arguments.RequirePositional(0, "product key")),
        _ => PrintUsage(),
    };
}
catch (CommandLineException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 2;
}
catch (ArgumentOutOfRangeException ex)
{
    Console.Error.WriteLine($"--channel must be 0-{ProductKeyFields.ChannelCount - 1} and --sequence 0-{ProductKeyFields.SerialsPerChannel - 1}: {ex.Message}");
    return 2;
}

static int CreateCurve(string keyFilePath, string iniSectionName, bool overwrite)
{
    if (File.Exists(keyFilePath) && !overwrite)
    {
        Console.Error.WriteLine($"{keyFilePath} already exists. Every key signed with it stops working if it's replaced, pass --force if you really mean it.");
        return 1;
    }

    Console.Error.WriteLine("Searching for a curve...");
    var signingKey = ProductKeyCurveGenerator.GenerateSigningKey(new SecureRandom());

    var signer = new ProductKeySigner(signingKey);
    var verifier = new ProductKeyVerifier(signingKey.Curve);
    var testKey = signer.CreateProductKey(0, 0);
    if (!verifier.TryVerifyProductKey(testKey, out _))
    {
        Console.Error.WriteLine("Generated curve failed its sign/verify self-test, nothing written.");
        return 1;
    }

    SigningKeyFile.Save(keyFilePath, signingKey);
    Console.Error.WriteLine($"Signing key written to {keyFilePath}");
    Console.Error.WriteLine();

    Console.Write(SigningKeyFile.FormatIniSection(signingKey.Curve, iniSectionName));
    return 0;
}

static int PrintIniSection(string keyFilePath, string iniSectionName)
{
    var signingKey = SigningKeyFile.Load(keyFilePath);
    Console.Write(SigningKeyFile.FormatIniSection(signingKey.Curve, iniSectionName));
    return 0;
}

static int GenerateProductKeys(string keyFilePath, CommandLineArguments arguments)
{
    var channelId = arguments.RequireIntOption("--channel");
    var firstSequence = arguments.RequireIntOption("--sequence");
    var count = arguments.GetIntOption("--count") ?? 1;
    var isUpgrade = arguments.HasFlag("--upgrade");

    if (count < 1 || firstSequence + count > ProductKeyFields.SerialsPerChannel)
        throw new CommandLineException($"--sequence {firstSequence} with --count {count} runs past the end of the channel.");

    var signer = new ProductKeySigner(SigningKeyFile.Load(keyFilePath));

    for (var sequence = firstSequence; sequence < firstSequence + count; sequence++)
    {
        var productKey = signer.CreateProductKey(channelId, sequence, isUpgrade);
        Console.WriteLine($"{channelId:D3}-{sequence:D6}\t{productKey}");
    }

    return 0;
}

static int VerifyProductKey(string keyFilePath, string productKey)
{
    var signingKey = SigningKeyFile.Load(keyFilePath);
    var verifier = new ProductKeyVerifier(signingKey.Curve);

    if (!verifier.TryVerifyProductKey(productKey, out var fields))
    {
        Console.WriteLine("invalid");
        return 1;
    }

    Console.WriteLine($"valid, channel {fields.ChannelId:D3}, sequence {fields.Sequence:D6}, upgrade {fields.IsUpgrade}");
    return 0;
}

static int DecodeProductKey(string productKey)
{
    if (!ProductKeyBase24.TryDecodeProductKey(productKey, out var value))
    {
        Console.WriteLine("not a product key");
        return 1;
    }

    var fields = ProductKeyLayout.UnpackFields(value);
    Console.WriteLine($"raw        0x{value:X29}");
    Console.WriteLine($"upgrade    {fields.IsUpgrade}");
    Console.WriteLine($"serial     {fields.Serial} (channel {fields.ChannelId:D3}, sequence {fields.Sequence:D6})");
    Console.WriteLine($"hash       0x{fields.Hash:X7}");
    Console.WriteLine($"signature  0x{fields.Signature:X14}");
    return 0;
}

static int PrintUsage()
{
    Console.Error.WriteLine($"""
        usage: ReLiveWP.ProductKeys.Cli <command> [--key-file <path>]

          new-curve [--force] [--section S]    make a new curve + signing key, print the server ini block
          print-ini [--section S]              print the server ini block again
          generate --channel N --sequence M [--count C] [--upgrade]
          verify <key>                         check a key against the signing key's curve
          decode <key>                         show a key's fields without checking the signature

        --key-file defaults to {SigningKeyFile.DefaultPath}
        --section defaults to {SigningKeyFile.DefaultIniSectionName}, use InviteKeys for signup invites
        """);
    return 2;
}
