using Discord.Interactions;
using Discord;
using Discord.WebSocket;
using MoonsecDeobfuscator.Deobfuscation;
using MoonsecDeobfuscator.Deobfuscation.Bytecode;

namespace MoonsecDeobfuscator
{
    public class Program
    {
        private DiscordSocketClient _client;

        public static void Main(string[] args)
            => new Program().RunBotAsync().GetAwaiter().GetResult();

        public async Task RunBotAsync()
{
    _client = new DiscordSocketClient();

    _client.Log   += LogAsync;
    _client.Ready += Client_Ready;

    var token = "YOUR-BOT-TOKEN-HERE";
    if (string.IsNullOrEmpty(token))
    {
        Console.WriteLine("Error: DISCORD_TOKEN environment variable not set.");
        return;
    }

    await _client.LoginAsync(TokenType.Bot, token);
    await _client.StartAsync();

    /* ===  NEW LINES  === */
    var interactions = new InteractionService(_client.Rest);
    await interactions.AddModuleAsync<SlashModule>(null);   // registers /deobfuscate

    _client.InteractionCreated += async (i) =>
    {
        var ctx = new SocketInteractionContext(_client, i);
        await interactions.ExecuteCommandAsync(ctx, null);
    };

    await interactions.RegisterCommandsGloballyAsync();     // publish slash command
    /* =================== */

    await Task.Delay(Timeout.Infinite);
}


        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private Task Client_Ready()
        {
            Console.WriteLine($"Bot is connected as: {_client.CurrentUser.Username}");
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            // Ignore bot and system messages
            if (message.Author.IsBot || message.Author.IsWebhook) return;
            
            // Command check: Use !deobf followed by the script content
            if (message.Content.StartsWith("!deobf "))
            {
                await message.Channel.TriggerTypingAsync(); // Shows "Bot is typing..."

                var scriptContent = message.Content.Substring("!deobf ".Length).Trim();
                
                if (string.IsNullOrEmpty(scriptContent))
                {
                    await message.Channel.SendMessageAsync("Please paste the Lua script content after the `!deobf` command.");
                    return;
                }

                // Define a temporary filename for the output file
                var tempFilePath = Path.Combine(Path.GetTempPath(), $"deobfuscated_{message.Id}.lua");
                
                try
                {
                    // 1. Run the Deobfuscation Logic
                    var result = new Deobfuscator().Deobfuscate(scriptContent);
                    
                    // 2. Write the disassembly result to the temporary file
                    string disassembledCode = new Disassembler(result).Disassemble();
                    File.WriteAllText(tempFilePath, disassembledCode);

                    // 3. Upload the file to Discord
                    await message.Channel.SendFileAsync(
                        filePath: tempFilePath,
                        text: $"✅ **Deobfuscation complete** for {message.Author.Mention}. The result is attached below:"
                    );
                }
                catch (Exception ex)
                {
                    // Send an error message if the deobfuscation or file writing fails
                    await message.Channel.SendMessageAsync($"⚠️ An error occurred during deobfuscation: ```{ex.Message}```");
                }
                finally
                {
                    // 4. CLEAN UP: Delete the temporary file from the server
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                }
            }
        }
    }
}
