using Discord.Commands;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NationStatesAPIBot.Commands
{
    public static class GuessGame
    {
        public static int Min { get; private set; }
        public static int Max { get; private set; }
        public static int Guesses { get; private set; }
        public static bool IsRunning { get; private set; }

        private static int number;
        private static Random _rnd = new Random();

        public static void InitGame(int min, int max)
        {
            Min = min;
            Max = max;
            number = _rnd.Next(Min, Max + 1);
            IsRunning = true;
        }

        ///-1 to low, 1 to high, 0 correct guess
        public static short AddGuess(int guess)
        {
            Guesses++;
            if (guess < number)
            {
                return -1;
            }
            else if (guess > number)
            {
                return 1;
            }
            IsRunning = false;
            return 0;
        }

        public static long GetPoints()
        {
            long result = Max - Min;
            if (result < 0)
            {
                result = -result;
            }
            return result / Guesses;
        }
    }

    public class Guess : ModuleBase<SocketCommandContext>
    {
        private ILogger<Guess> _logger;

        public Guess(ILogger<Guess> logger)
        {
            _logger = logger;
        }

        [Command("guess", false), Alias("g")]
        public async Task DoGuessAsync(params string[] args)
        {
            try
            {
                if (args.Length == 1)
                {
                    if (GuessGame.IsRunning)
                    {
                        if (int.TryParse(args[0], out int guess))
                        {
                            var ret = GuessGame.AddGuess(guess);
                            if (ret == -1)
                            {
                                await ReplyAsync($"{Context.User.Mention} Your guess was too low.");
                            }
                            else if (ret == 0)
                            {
                                await ReplyAsync($"{Context.User.Mention} Correct. You won. Result: {GuessGame.Guesses} guesses, {GuessGame.GetPoints()} point(s). (points are not stored)");
                            }
                            else if (ret == 1)
                            {
                                await ReplyAsync($"{Context.User.Mention} Your guess was too high.");
                            }
                        }
                        else
                        {
                            await ReplyAsync($"Whoops...{args[0]} (valid) number. Numbers needs to be between {int.MinValue} and {int.MaxValue}");
                        }
                    }
                    else
                    {
                        await ReplyAsync("No GuessGame running. Start one with /guess <min> <max>");
                        return;
                    }
                }
                else if (args.Length == 2)
                {
                    if (GuessGame.IsRunning)
                    {
                        await ReplyAsync($"There is already a guess game running. Participate in the current one or wait until it ends to start a new one.");
                        return;
                    }
                    if (!int.TryParse(args[0], out int min) && min > -10000001)
                    {
                        await ReplyAsync($"Whoops... {args[0]} is not a (valid) number. Numbers needs to be between -10000000 and 10000000");
                        return;
                    }
                    if (!int.TryParse(args[1], out int max) && max < 10000001)
                    {
                        await ReplyAsync($"Whoops... {args[1]} is not a (valid) number.  Numbers needs to be between -10000000 and 10000000");
                        return;
                    }
                    if (min < max)
                    {
                        GuessGame.InitGame(min, max);
                        await ReplyAsync($"Your guess game has been created. Min: {min} Max: {max}");
                    }
                    else if (min == max)
                    {
                        await ReplyAsync("Whoops...Min and Max are equal. That is not supposed to be that way. :joy:");
                    }
                    else
                    {
                        await ReplyAsync("Whoops...Max is less than Min ? That does not seem right. :thinking:");
                    }
                }
                else
                {
                    await ReplyAsync("Guess what? I don't know what you want me to do.");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error:", ex);
            }
        }
    }
}