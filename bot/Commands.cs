using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bot
{
    public class CommandModule : ModuleBase<SocketCommandContext>
    {
        [Command("hello")]
        public async Task HelloCommand()
        {
            await ReplyAsync("Hello! How can I assist you?");
        }

        [Command("ping")]
        public async Task CheckCommand()
        {
            await ReplyAsync("Pong!");
        }

        [Command("help")]
        public async Task HelpCommand()
        {
            await ReplyAsync("`This is a help message that you can modify.`");
        }

        //[Command("deletecommands")]
        //public async Task DeleteCommands()
        //{
        //    // Assuming you have registered slash commands using SlashCommandsExtension
        //    var commandsExtension = _client.GetCommandsNext();
        //    if (commandsExtension != null)
        //    {
        //        await commandsExtension.SlashCommandsExtension.DeleteAllCommandsAsync();
        //        await ReplyAsync("All slash commands deleted.");
        //    }
        //    else
        //    {
        //        await ReplyAsync("Slash commands extension not found.");
        //    }
        //}
    }
}
