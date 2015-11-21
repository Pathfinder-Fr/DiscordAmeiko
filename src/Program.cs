using Discord;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordAmeiko
{
    class Program
    {
        private static readonly Regex regex = new Regex(@"^(?<Formula>(?<Roll>\d+(d\d+)?)(\s*(?<Roll>[\+\-]\d+(d\d+)?))*)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        private static DiscordBotClient client;

        static void Main(string[] args)
        {
            if (args == null || args.Length != 2)
            {
                Console.WriteLine("usage: {0}.exe [username] [password]", System.Reflection.Assembly.GetEntryAssembly().CodeBase);
                return;
            }

            client = new DiscordBotClient();
            client.CommandChar = '!';
            client.LogMessage += (s, e) => Console.WriteLine($"[{e.Severity}] {e.Source}: {e.Message}");

            client.Connect(args[0], args[1]).Wait();
            client.CreateCommand("lance").Do(Roll);
            client.CreateCommand("salut").Do(Salut);
            Console.ReadKey();
        }

        private async static Task Roll(CommandEventArgs e)
        {
            var formula = e.CommandText.Substring(e.Command.Text.Length + 1).Trim();

            var match = regex.Match(formula);

            if (match.Success)
            {
                var result = 0;
                var rolls = new List<int>();

                var rollCount = 0;

                var r = new Random();
                foreach (Capture dice in match.Groups["Roll"].Captures)
                {
                    rollCount++;
                    if (rollCount > 10)
                    {
                        await client.SendMessage(e.Channel, "Trop de jets ! 10 jets maximum pour l'instant");
                        return;
                    }

                    var diceFormula = dice.Value;
                    var hasDice = diceFormula.IndexOf('d') != -1;
                    var diceFormulaParts = diceFormula.Split('d');
                    int roll;
                    if (diceFormulaParts.Length == 1)
                    {
                        if (hasDice)
                        {
                            // "d20"                            
                            roll = r.Next(1, int.Parse(diceFormulaParts[0]));
                            rolls.Add(roll);
                            result += roll;
                        }
                        else
                        {
                            // "10"
                            result += int.Parse(diceFormulaParts[0]);
                        }
                    }
                    else if (diceFormulaParts.Length == 2)
                    {
                        var count = int.Parse(diceFormulaParts[0]);

                        var coef = 1;
                        if (count < 0)
                        {
                            count = -count;
                            coef = -1;
                        }

                        if (count > 10)
                        {
                            await client.SendMessage(e.Channel, "Trop de dés ! 10 dés maximum par jet pour l'instant");
                            return;
                        }

                        for (var i = 0; i < count; i++)
                        {
                            var faces = int.Parse(diceFormulaParts[1]);

                            if(faces > 1000)
                            {
                                await client.SendMessage(e.Channel, "Un dé avec autant de faces ? Jamais vu...");
                                return;
                            }

                            roll = r.Next(1, faces);
                            rolls.Add(roll);
                            result += roll * coef;
                        }
                    }
                }

                string content = string.Format("{3} lance {0} et obtient {1} ({2})", formula, result, string.Join(", ", rolls), e.User.Name);

                await client.SendMessage(e.Channel, content);
            }
        }

        private async static Task Salut(CommandEventArgs e)
        {
            await client.SendMessage(e.Channel, $"Salut {e.User.Name}, bienvenue à l'auberge du Dragon Rouillé de Pointesable ! Pour l'instant tu peux juste lancer quelques dés avec la commande !lance (par ex. !lance 1d20), mais le meilleur est à venir.");
        }
    }
}
