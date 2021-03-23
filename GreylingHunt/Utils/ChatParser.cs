using System.Linq;
using System.Text.RegularExpressions;

namespace GreylingHunt.Utils
{
    public static class ChatParser
    {
        static public void ParsePlayerInput(string text)
        {
            string[] textSplit = text.Split(' '); // Split up args
            bool onScreen = false;
            int screenPos = 1;
            if (textSplit.Length > 1)
            { // Make sure it's more than 1 word
                if (textSplit[0] == "/announce")
                { // Check if it's our command
                    string msg = ""; // Make msg
                    var re = new Regex("(?<=\")[^\"]*(?=\")|[^\" ]+");
                    var strings = re.Matches(text).Cast<Match>().Select(m => m.Value).ToArray();
                    msg = strings[1];
                    if (strings.Length >= 3)
                    {
                        onScreen = bool.Parse(strings[2]);
                    }
                    if (strings.Length == 4)
                    {
                        screenPos = int.Parse(strings[3]);
                    }
                    Log.LogInfo(screenPos);
                    // Send msg over RPC to server 
                    ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), "RequestServerAnnouncement", msg, onScreen, screenPos);
                }
            }
        }
    }
}
