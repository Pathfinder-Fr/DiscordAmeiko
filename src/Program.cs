using Discord;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordAmeiko
{
    class Program
    {
        private static readonly Regex regex = new Regex(@"^(?<Formula>(?<Roll>\d+(d\d+(g\d+)?)?)(\s*(?<Roll>[\+\-]\d+(d\d+(g\d+)?)?))*)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

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

            if (!match.Success)
            {
                await client.SendMessage(e.Channel, "Formule non reconnue. Format de formule : !lance XdY(gZ) [(+|-)X(dY(gZ)) ...]. Exemples : 1d6, 1d20+3, 4d6g3 (garde les 3 meilleurs)");
                return;
            }

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

                // X OR dY OR XdY OR XdYkZ
                var diceFormula = dice.Value;

                var count = 1;
                var faces = 0;
                var keep = -1;

                var diceIndex = diceFormula.IndexOf("d", StringComparison.OrdinalIgnoreCase);
                if (diceIndex != -1)
                {
                    if (diceIndex == 0)
                    {
                        // dY
                        count = 1;
                    }
                    else
                    {
                        // XdY
                        count = int.Parse(diceFormula.Substring(0, diceIndex), NumberStyles.AllowLeadingSign);
                    }

                    var keepIndex = diceFormula.IndexOf("g", diceIndex, StringComparison.OrdinalIgnoreCase);
                    if (keepIndex != -1)
                    {
                        // XdYkZ
                        faces = int.Parse(diceFormula.Substring(diceIndex + 1, keepIndex - diceIndex - 1), NumberStyles.None);
                        keep = int.Parse(diceFormula.Substring(keepIndex + 1), NumberStyles.None);
                    }
                    else
                    {
                        // XdY
                        faces = int.Parse(diceFormula.Substring(diceIndex + 1), NumberStyles.None);
                    }
                }
                else
                {
                    // X
                    result += int.Parse(diceFormula, NumberStyles.AllowLeadingSign);
                }

                if (faces > 1000)
                {
                    await client.SendMessage(e.Channel, "Un dé avec autant de faces ? Jamais vu...");
                    return;
                }

                if (count > 10)
                {
                    await client.SendMessage(e.Channel, "Trop de dés ! 10 dés maximum par jet pour l'instant");
                    return;
                }

                // handle sign
                var coef = 1;
                if (count < 0)
                {
                    count = -count;
                    coef = -1;
                }

                if (faces != 0)
                {
                    // roll {count} dices with {faces} sides
                    var captureRolls = Enumerable.Range(0, count).Select(i => r.Next(1, faces + 1));

                    if (keep != -1)
                    {
                        // keep only {keep} best throws
                        captureRolls = captureRolls.OrderByDescending(x => x).Take(keep);
                    }

                    // sums rolls
                    foreach (var roll in captureRolls)
                    {
                        rolls.Add(roll);
                        result += roll*coef;
                    }
                }
            }

            var content = string.Format("{3} lance {0} et obtient {1} ({2})", formula, result, string.Join(", ", rolls), e.User.Name);

            await client.SendMessage(e.Channel, content);
        }

        private async static Task Salut(CommandEventArgs e)
        {
            await client.SendMessage(e.Channel, $"Salut {e.User.Name}, bienvenue à l'auberge du Dragon Rouillé de Pointesable ! Pour l'instant tu peux juste lancer quelques dés avec la commande !lance (par ex. !lance 1d20), mais le meilleur est à venir.");
        }
    }
}
