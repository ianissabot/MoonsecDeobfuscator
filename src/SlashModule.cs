using Discord;
using Discord.Interactions;
using MoonsecDeobfuscator.Deobfuscation;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

public class SlashModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("deobfuscate", "Upload a Moonsec-obfuscated .lua file")]
    public async Task Deobfuscate(IAttachment file)
    {
        if (!file.Filename.EndsWith(".lua"))
        {
            await RespondAsync("❗ I need a `.lua` file.", ephemeral: true);
            return;
        }

        await DeferAsync();                         // "Bot is thinking..."

        var inPath  = Path.GetTempFileName();
        var outPath = Path.ChangeExtension(inPath, ".lua");

        try
        {
            using var http = new HttpClient();
            await File.WriteAllBytesAsync(inPath,
                await http.GetByteArrayAsync(file.Url));

            byte[] bytecode = new Deobfuscator().Deobfuscate(inPath);
            string cleaned  = new Disassembler().Disassemble(bytecode);
            await File.WriteAllTextAsync(outPath, cleaned);

            await FollowupWithFileAsync(outPath,
                file.Filename.Replace(".lua", "_clean.lua"));
        }
        catch (Exception ex)
        {
            await FollowupAsync($"⚠️ Failed: ```{ex.Message}```");
        }
        finally
        {
            File.Delete(inPath);
            File.Delete(outPath);
        }
    }
}
